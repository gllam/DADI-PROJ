using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DIDAWorker;
using Grpc.Core;
using Grpc.Net.Client;

namespace DIDAStorageUI
{
    struct UpdateRecord
    {
        public int i;
        public int[] timeStamp;
        public int[] prev;
        public string key;
        public string value;
    }

    class StorageService : DIDAStorageService.DIDAStorageServiceBase
    {

        Dictionary<string, List<DIDARecord>> data = new Dictionary<string, List<DIDARecord>>();
        private readonly object dataLock = new object();
        int maxVersions = 5; //dummy
        readonly int replicaId;
        int maxStorages;
        string name;
        int gossipDelay;
        int operationId = 0;
        private readonly object operationIdLock = new object();

        private readonly object gossipMessageLock = new object();

        Dictionary<DIDAStorageNode, DIDAStorageService.DIDAStorageServiceClient> storageMap = new Dictionary<DIDAStorageNode, DIDAStorageService.DIDAStorageServiceClient>();
        private readonly object storageMapLock = new object();

        Dictionary<string, int[]> valueTimeStamp = new Dictionary<string, int[]>();//the correct/real one
        private readonly object valueTimeStampLock = new object();

        Dictionary<string, int[]> replicaTimeStamp = new Dictionary<string, int[]>();//the one that has the data that is in the log
        private readonly object replicaTimeStampLock = new object();

        Dictionary<int,UpdateRecord> updateLog = new Dictionary<int, UpdateRecord>();
        private readonly object updateLogLock = new object();

        private readonly object updateIfValueIsLock = new object();

        //TODO updateif ; S2S.proto(pm cliente) ; data consistency ; fault tolerance ; gossip

        public StorageService(int replicaid, int gossipDelay, string name)
        {
            this.gossipDelay = gossipDelay;
            this.replicaId = replicaid;
            this.name = name;
        }

        public override Task<ListServerStorageReply> ListServer(StorageStatusEmpty request, ServerCallContext context)
        {
            return Task.FromResult(LiServer());
        }

        private ListServerStorageReply LiServer()
        {
            return new ListServerStorageReply { SereverDataToSTring = this.ToString() };
        }

        public override Task<StorageStatusReply> Status(StorageStatusEmpty request, ServerCallContext context)
        {
            return Task.FromResult(StatusOperation());
        }

        private StorageStatusReply StatusOperation()
        {
            Console.WriteLine("Storage: " + this.name + " -> I am alive!");
            StorageStatusReply reply = new StorageStatusReply { Success = true };
            return reply;
        }

        public override Task<DIDAVersion> populate(DIDAWriteRequest request, ServerCallContext context)
        {
            return Task.FromResult(Populate(request));
        }

        private DIDAVersion Populate(DIDAWriteRequest request)
        {
            int[] timeStampValue = GetValueTimeStampValue(request.Id);
            timeStampValue[replicaId] += 1;
            DIDAVersion version = UpdateData(request.Id, request.Val);
            MergeTSIntoValueTimeStamp(timeStampValue, request.Id);

            SendGossipToAllStorages(timeStampValue, request.Id, request.Val, version);//When this function ends all the gossipMessages are in the channels
            return version;
        }

        public override Task<sendUpdateRequestReply> sendUpdateRequest(sendUpdateRequestReq request, ServerCallContext context)
        {
            return Task.FromResult<sendUpdateRequestReply>(UpdateRequest(request));
        }

        private sendUpdateRequestReply UpdateRequest(sendUpdateRequestReq request)
        {
            sendUpdateRequestReply reply = new sendUpdateRequestReply { };
            lock (replicaTimeStampLock)
            {
                replicaTimeStamp[request.Key][replicaId] += 1;

                lock (updateLogLock)
                {
                    int id;
                    lock (operationIdLock)
                    {
                        operationId += 1;
                        id = operationId;
                    }
                    List<int> buffer = new List<int>();
                    List<int> timeStamp = new List<int>();
                    foreach (int n in request.Tmv.NumberUpdates)
                    {
                        buffer.Add(n);
                        timeStamp.Add(n);
                    }
                    timeStamp[replicaId] = replicaTimeStamp[request.Key][replicaId];
                    UpdateRecord record = new UpdateRecord
                    {
                        i = replicaId,
                        key = request.Key,
                        value = request.Value,
                        timeStamp = timeStamp.ToArray()
                    };
                    record.prev = buffer.ToArray();

                    updateLog.Add(id, record);
                    foreach (int update in record.timeStamp)
                    {
                        reply.Tmv.NumberUpdates.Add(update);
                    }
                    reply.Id = id;
                }
            }
            return reply;
        }

        public override Task<sendUpdateValidationReply> sendUpdateValidation(sendUpdateAck request, ServerCallContext context)
        {
            return Task.FromResult<sendUpdateValidationReply>(UpdateValidation(request));
        }

        private sendUpdateValidationReply UpdateValidation(sendUpdateAck request)
        {
            UpdateRecord record = getUpdateRecord(request.Id);
            sendUpdateValidationReply reply;
            if (request.Success)
            {
                int[] timeStampValue = MergeTSIntoValueTimeStamp(record.timeStamp, record.key);
                DIDAVersion version = UpdateData(record.key, record.value);
                reply = new sendUpdateValidationReply { Version = version };
                foreach(int n in timeStampValue)
                {
                    reply.Tmv.NumberUpdates.Add(n);
                }
                
                SendGossipToAllStorages(timeStampValue, record.key, record.value, version);//When this function ends all the gossipMessages are in the channels
                RemoveFromUpdateLog(request.Id);
                Monitor.Pulse(updateIfValueIsLock);
                return reply;
            }

            lock (gossipMessageLock)
            {
                int[] valueTimeStampValue = GetValueTimeStampValue(record.key);
                int[] buffer = GetValueTimeStampValue(record.key);

                while (EqualsArrayInt(valueTimeStampValue, buffer))
                {
                    Monitor.Wait(gossipMessageLock);
                    buffer = GetValueTimeStampValue(record.key);
                }
                DIDAVersion version = new DIDAVersion { ReplicaId = replicaId, VersionNumber = -1 };
                reply = new sendUpdateValidationReply { Version = version };
                foreach (int n in buffer)
                {
                    reply.Tmv.NumberUpdates.Add(n);
                }
                SendGossipToAllStorages(buffer, record.key, record.value, version);//When this function ends all the gossipMessages are in the channels
                RemoveFromUpdateLog(request.Id);
                Monitor.Pulse(updateIfValueIsLock);
            }
            return reply;
        }

        public override Task<sendUpdateValidationReply> updateIfValueIs(DIDAUpdateIfRequest request, ServerCallContext context)
        {
            return Task.FromResult<sendUpdateValidationReply>(UpdateIfValueIs(request));
        }

        private sendUpdateValidationReply UpdateIfValueIs(DIDAUpdateIfRequest request)
        {
            //Garantir sucesso -> Mandar random messages to all channels so we can get all gossips(FIFO channels)
            sendUpdateValidationReply reply; //= new sendUpdateValidationReply { };

            lock (replicaTimeStampLock)//Preventing start of new write operations
            {
                foreach (var s in storageMap)
                {
                    try
                    {
                        s.Value.ping(new EmptyAnswer { });
                    }
                    catch (Exception) { }
                } //When this finishes this storage has received all updates regarding this key

                lock (updateIfValueIsLock) //This lock is only to prevent running the while over and over again
                {
                    while(GetUpdateLogSizeKey(request.Id) != 0)
                    {
                        Monitor.Wait(updateIfValueIsLock);
                    }
                    lock (dataLock)
                    {
                        if (data[request.Id][data[request.Id].Count - 1].Val == request.Oldvalue)
                        {
                            DIDAVersion version = UpdateData(request.Id, request.Newvalue);
                            reply = new sendUpdateValidationReply { Version = version };
                            int[] timeStampValue = GetValueTimeStampValue(request.Id);
                            timeStampValue[replicaId] = timeStampValue[replicaId] + 1;

                            MergeTSIntoValueTimeStamp(timeStampValue, request.Id);
                            MergeTSIntoReplicaTimeStamp(timeStampValue, request.Id);

                            foreach (int n in timeStampValue)
                            {
                                reply.Tmv.NumberUpdates.Add(n);
                            }

                            SendGossipToAllStorages(timeStampValue, request.Id, request.Newvalue, version);//When this function ends all the gossipMessages are in the channels
                        }
                        else
                        {
                            DIDAVersion version = new DIDAVersion { ReplicaId = -1, VersionNumber = -1 };
                            int[] timeStampValue = GetValueTimeStampValue(request.Id);
                            reply = new sendUpdateValidationReply { Version = version };
                            foreach (int n in timeStampValue)
                            {
                                reply.Tmv.NumberUpdates.Add(n);
                            }
                        }
                    }
                }
            }
            return reply;
        }

        private int GetUpdateLogSizeKey(string key)
        {
            int size = 0;
            lock (updateLogLock)
            {
                foreach(UpdateRecord record in updateLog.Values)
                {
                    if (record.key == key)
                        size += 1;
                }
            }
            return size;
        }

        private void SendGossipToAllStorages(int[] timeStampValue, string key, string value, DIDAVersion version)
        {
            gossipMessage request = new gossipMessage {
                Record = new DIDARecordReply { Id = key, Val = value, Version = version}
            };
            foreach(int i in timeStampValue)
            {
                request.Tmv.NumberUpdates.Add(i);
            }

            System.Threading.Thread.Sleep(gossipDelay);
            foreach (var s in storageMap)
            {
                try { 
                    s.Value.gossipAsync(request);
                }
                catch (Exception) { }

            }
        }

        public override Task<EmptyAnswer> gossip(gossipMessage request, ServerCallContext context)
        {
            return Task.FromResult<EmptyAnswer>(Gossip(request));
        }

        //testar gossip com dead storages
        private EmptyAnswer Gossip(gossipMessage request)
        {
            List<int> timeStampValueGossip = new List<int>();
            foreach(int i in request.Tmv.NumberUpdates)
            {
                timeStampValueGossip.Add(i);
            }
            if(IsTSBigger(timeStampValueGossip.ToArray() , GetValueTimeStampValue(request.Record.Id)))
            {
                UpdateData(request.Record.Id, request.Record.Val);
                MergeTSIntoValueTimeStamp(timeStampValueGossip.ToArray(), request.Record.Id);
                MergeTSIntoReplicaTimeStamp(timeStampValueGossip.ToArray(), request.Record.Id);
                Monitor.PulseAll(gossipMessageLock);
            }
            return new EmptyAnswer { };
        }

        public override Task<sendReadRequestReply> sendReadRequest(sendReadRequestReq request, ServerCallContext context)
        {
            return Task.FromResult<sendReadRequestReply>(Read(request));
        }

        private sendReadRequestReply Read(sendReadRequestReq request)
        {
            sendReadRequestReply reply;
            int[] valueTimeStampValue;
            List<int> timeStampReceived = new List<int>();

            foreach(int i in request.Tmv.NumberUpdates)
            {
                timeStampReceived.Add(i);
            }

            lock (valueTimeStampLock)
            {
                valueTimeStampValue = new int[maxStorages];
                valueTimeStamp[request.Key].CopyTo(valueTimeStampValue, 0);
            }
            if (IsTSBigger(valueTimeStampValue,timeStampReceived.ToArray()))
            {
                DIDARecord record = SearchRecord(request.Key,request.Version);
                reply = new sendReadRequestReply {
                    Key = request.Key,
                    Value = record.Val,
                    Version = new DIDAVersion { ReplicaId = record.Version.ReplicaId, VersionNumber = record.Version.VersionNumber}
                };
                foreach(int n in valueTimeStampValue)
                {
                    reply.Tmv.NumberUpdates.Add(n);
                }
                return reply;
            }
            lock (gossipMessageLock)
            {
                int[] buffer = GetValueTimeStampValue(request.Key);

                while (!IsTSBigger(buffer, timeStampReceived.ToArray()))
                {
                    Monitor.Wait(gossipMessageLock);
                    buffer = GetValueTimeStampValue(request.Key);
                }

                DIDARecord record = SearchRecord(request.Key, request.Version);
                reply = new sendReadRequestReply
                {
                    Key = request.Key,
                    Value = record.Val,
                    Version = new DIDAVersion { ReplicaId = record.Version.ReplicaId, VersionNumber = record.Version.VersionNumber }
                };
                foreach (int n in valueTimeStampValue)
                {
                    reply.Tmv.NumberUpdates.Add(n);
                }
            }
            return reply;
        }

        private DIDARecord SearchRecord(string key, DIDAVersion version)
        {
            DIDARecord reply = new DIDARecord { Id = key, Val = null, Version = new DIDAWorker.DIDAVersion { ReplicaId = version.ReplicaId, VersionNumber = version.VersionNumber } }; ;
            lock (dataLock)
            {
                if(version.VersionNumber == -1) {
                    reply = data[key][data[key].Count - 1];
                }
                else
                {
                    foreach (DIDARecord saved in data[key])
                    {
                        if (saved.Version.VersionNumber == version.VersionNumber && saved.Version.ReplicaId == version.ReplicaId)
                        {
                            reply = saved;
                            break;
                        }
                    }
                }
            }
            return reply;
        }

        //Check if timeStamp1 >= timeSTamp2
        private bool IsTSBigger(int[] timeStamp1, int[] timeStamp2)
        {
            int size = timeStamp1.Length;
            for(int i = 0; i < size; i++)
            {
                if (timeStamp2[i] > timeStamp1[i])
                    return false;
            }
            return true;
        }

        private bool EqualsArrayInt(int[] a, int[] b)
        {
            int size = a.Length;
            for(int i = 0; i < size; i++)
            {
                if (a[i] != b[i])
                    return false;
            }
            return true;
        }

        private int[] GetValueTimeStampValue(string key)
        {
            int[] valueTimeStampValue;
            lock (valueTimeStampLock)
            {
                valueTimeStampValue = new int[maxStorages];
                if (!valueTimeStamp.ContainsKey(key))
                    valueTimeStamp[key] = new int[maxStorages];

                valueTimeStamp[key].CopyTo(valueTimeStampValue, 0);
            }
            return valueTimeStampValue;
        }

        private void RemoveFromUpdateLog(int operationId)
        {
            lock (updateLogLock)
            {
                updateLog.Remove(operationId);
            }
        }

        private int getUpdateLogSize()
        {
            int size;
            lock (updateLogLock)
            {
                size = updateLog.Count;
            }
            return size;
        }

        private DIDAVersion UpdateData(string key, string value)
        {
            DIDARecord record = new DIDARecord
            {
                Id = key,
                Val = value,
                Version = new DIDAWorker.DIDAVersion
                {
                    ReplicaId = replicaId
                }
            };
            lock (dataLock)
            {
                if (data.ContainsKey(key)) {

                    record.Version.VersionNumber = data[key][data[key].Count - 1].Version.VersionNumber + 1;
                }
                else
                {
                    record.Version.VersionNumber = 1;
                    data.Add(key, new List<DIDARecord>());
                }

                data[key].Add(record);
                if (data[key].Count > maxVersions) data[key].RemoveAt(0);
            }
            Console.WriteLine("Write successful -> " + record.Id + ":" + record.Val);
            return new DIDAVersion { ReplicaId = record.Version.ReplicaId, VersionNumber = record.Version.VersionNumber };
        }

        private int[] MergeTSIntoValueTimeStamp(int[] timeStamp,string key)
        {
            int[] buffer;
            lock (valueTimeStampLock)
            {
                buffer = new int[maxStorages];
                for (int i = 0; i < maxStorages; i++)
                {
                    valueTimeStamp[key][i] = valueTimeStamp[key][i] >= timeStamp[i] ? valueTimeStamp[key][i] : timeStamp[i];
                }
                valueTimeStamp[key].CopyTo(buffer,0);
            }
            return buffer;
        }

        private int[] MergeTSIntoReplicaTimeStamp(int[] timeStamp, string key)
        {
            int[] buffer;
            lock (replicaTimeStampLock)
            {
                buffer = new int[maxStorages];
                for (int i = 0; i < maxStorages; i++)
                {
                    replicaTimeStamp[key][i] = replicaTimeStamp[key][i] >= timeStamp[i] ? replicaTimeStamp[key][i] : timeStamp[i];
                }
                replicaTimeStamp[key].CopyTo(buffer, 0);
            }
            return buffer;
        }

        private UpdateRecord getUpdateRecord(int id)
        {
            return updateLog[id];
        }

        public override Task<DIDARecordReply> read(DIDAReadRequest request, ServerCallContext context)
        {
            return Task.FromResult<DIDARecordReply>(ReadData(request));
        }

        private DIDARecordReply ReadData(DIDAReadRequest request)
        {
            DIDARecordReply reply = new DIDARecordReply
            {
                Id = request.Id,
                Version = request.Version
            };
            lock (this)
            {
                if (data.ContainsKey(request.Id))
                {
                    if (request.Version.VersionNumber == -1)
                    {
                        var lastRecord = data[request.Id][data[request.Id].Count - 1];
                        reply.Val = lastRecord.Val;
                        reply.Version.VersionNumber = lastRecord.Version.VersionNumber;
                        reply.Version.ReplicaId = lastRecord.Version.ReplicaId;
                    }
                    else
                    {
                        DIDARecord record = data[request.Id].Find(x => x.Version.VersionNumber == request.Version.VersionNumber && x.Version.ReplicaId == request.Version.ReplicaId);
                        if(record.Id == request.Id) {
                            reply.Val = record.Val;
                        } else
                        {
                            reply.Version.VersionNumber = -1;
                            reply.Version.ReplicaId = -1;
                        }
                        
                    }
                }
            }
            Console.WriteLine(this);
            return reply;
        }

        private void CreateTimeStampKey(string key)
        {
            valueTimeStamp.Add(key, new int[storageMap.Count]);
        }

        internal void AddStorage(string storage)
        {
            string[] storageUrl = storage.Split("|");
            string[] hostport = storageUrl[1].Split("//")[1].Split(":");
            DIDAStorageNode node = new DIDAStorageNode
            {
                serverId = storageUrl[0],
                host = hostport[0],
                port = Convert.ToInt32(hostport[1])
            };
            GrpcChannel channel = GrpcChannel.ForAddress("http://" + node.host + ":" + node.port );
            storageMap.Add(node, new DIDAStorageService.DIDAStorageServiceClient(channel));
        }

        public override string ToString()
        {
            return "Storage " + name + " ReplicaId " + replicaId + " GossipDelay " + gossipDelay + "\r\nItems:\r\n" + ListData();
        }

        public string ListData()
        {
            string s_data = "";
            foreach (var d in data)
            {
                s_data += d.Key + " ";
                foreach(var value in d.Value)
                {
                    s_data += value.Val + ":" + value.Version.VersionNumber + " ";
                }
                s_data += "\r\n";
            }
            return s_data;
        }

        internal void SetMaxStorages()
        {
            this.maxStorages = storageMap.Count;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            int replicaid = Convert.ToInt32(args[0]);
            string storageName = args[1];
            string host = args[2];
            int port = Convert.ToInt32(args[3]);
            int gossipDelay = Convert.ToInt32(args[4]);
            StorageService storageService = new StorageService(replicaid, gossipDelay, storageName);
            for (int i = 5; i < args.Length; i++)
            {
                storageService.AddStorage(args[i]);
            }
            storageService.SetMaxStorages();
            Console.WriteLine("Insecure Storage server '" + storageName + "' | hostname: " + host + " | port " + port);
            Server server = new Server
            {
                Services = { DIDAStorageService.BindService(storageService) },
                Ports = { new ServerPort(host, port, ServerCredentials.Insecure) }
            };
            server.Start();
            Console.ReadKey();
            server.ShutdownAsync().Wait();
        }
    }
}
