﻿using DIDAWorker;
using System;
using Grpc.Net.Client;
using System.Collections.Generic;
using Google.Protobuf.Collections;
using OperatorRunner;

namespace Worker
{

    public struct Client
    {
        public int id;
        public DIDAStorageNode storageNode;
        public DIDAStorageService.DIDAStorageServiceClient client;
    }

    public class StorageProxy : IDIDAStorage
    {
        Dictionary<int, Client> _clients = new Dictionary<int, Client>();

        int allClients;

        MetaRecord _meta;


        public StorageProxy(DIDAStorageNode[] storageNodes, MetaRecord metaRecord, int allClients)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            int k = 0;
            this.allClients = allClients;
            
            foreach (DIDAStorageNode n in storageNodes)
            {
                GrpcChannel channel = GrpcChannel.ForAddress("http://" + n.host + ":" + n.port);
                _clients[n.serverId.GetHashCode()] = new Client { id = k, storageNode = n, client = new DIDAStorageService.DIDAStorageServiceClient(channel) };
                k++;
            }
            _meta = metaRecord;
        }

        // THE FOLLOWING 3 METHODS ARE THE ESSENCE OF A STORAGE PROXY
        // IN THIS EXAMPLE THEY ARE JUST CALLING THE STORAGE 
        // IN THE COMLPETE IMPLEMENTATION THEY NEED TO:
        // 1) LOCATE THE RIGHT STORAGE SERVER
        // 2) DEAL WITH FAILED STORAGE SERVERS
        // 3) CHECK IN THE METARECORD WHICH ARE THE PREVIOUSLY READ VERSIONS OF DATA
        // 4) RECORD ACCESSED DATA INTO THE METARECORD

        private int LocateStorage(string id)
        {
            int hash = id.GetHashCode();
            int closer = 0;
            foreach(int i in _clients.Keys)
            {
                if (Math.Abs(hash - i) < Math.Abs(hash - closer) || closer == 0)
                {
                    closer = i;
                }
            }
            return closer;
        }

        public virtual DIDAWorker.DIDARecordReply read(DIDAWorker.DIDAReadRequest r)
        {
            if (!_meta.timeStamp.ContainsKey(r.Id))
                CreateTimeStampKey(r.Id);

            int storageHash = LocateStorage(r.Id);
            Client storage = _clients[storageHash];
            try
            {
                int versionNumber;
                int replicaIdToSend;
                if (_meta.lastChanges.ContainsKey(r.Id))
                {
                    versionNumber = _meta.lastChanges[r.Id].VersionNumber;
                    replicaIdToSend = _meta.lastChanges[r.Id].ReplicaId;
                }
                else { versionNumber = -1; replicaIdToSend = -1; }

                List<int> buffer = new List<int>();
                foreach(int n in _meta.timeStamp[r.Id])
                {
                    buffer.Add(n);
                }

                sendReadRequestReq request = new sendReadRequestReq
                {
                    Key = r.Id,
                    Version = new DIDAVersion
                    {
                        VersionNumber = r.Version.VersionNumber == -1 ? versionNumber : r.Version.VersionNumber,
                        ReplicaId = r.Version.ReplicaId == -1 ? replicaIdToSend : r.Version.ReplicaId
                    },
                };
                request.Tmv = new TimeStampValue { };
                request.Tmv.NumberUpdates.Add(buffer);


                sendReadRequestReply reply = storage.client.sendReadRequest(request);

                UpdateMetaTimeStamp(reply.Tmv, r.Id);
                _meta.lastChanges[r.Id] = new DIDAWorker.DIDAVersion { ReplicaId = reply.Version.ReplicaId, VersionNumber = reply.Version.VersionNumber};
                return new DIDAWorker.DIDARecordReply { Id = reply.Key, Val = reply.Value, Version = { VersionNumber = reply.Version.VersionNumber, ReplicaId = reply.Version.ReplicaId } };
            }
            catch (Exception e)
            {
                Console.WriteLine("Storage with ID " + storage.storageNode.serverId + " disconnected");
                _clients.Remove(storageHash);
                return read(r);
            }
        }

        public virtual DIDAWorker.DIDAVersion write(DIDAWorker.DIDAWriteRequest r)
        {
            if (!_meta.timeStamp.ContainsKey(r.Id))
                CreateTimeStampKey(r.Id);
            int storageHash = LocateStorage(r.Id);
            Client storage = _clients[storageHash];
            try
            {
                sendUpdateRequestReq request = new sendUpdateRequestReq { Key = r.Id, Value = r.Val };
                request = CreateTimeStampValueRepeated(r.Id, request);

                sendUpdateRequestReply reply = storage.client.sendUpdateRequest(request);

                sendUpdateValidationReply replyValidation = storage.client.sendUpdateValidation(new sendUpdateAck { Id = reply.Id, Success = CompareTimeStamps(request.Tmv.NumberUpdates, reply.Tmv.NumberUpdates) });
                while (replyValidation.Version.VersionNumber == -1)
                { 
                    replyValidation = storage.client.sendUpdateValidation(new sendUpdateAck {Id = reply.Id, Success = CompareTimeStamps(request.Tmv.NumberUpdates, replyValidation.Tmv.NumberUpdates) });
                }
                UpdateMetaTimeStamp(replyValidation.Tmv, r.Id);
                DIDAWorker.DIDAVersion version = new DIDAWorker.DIDAVersion { VersionNumber = replyValidation.Version.VersionNumber, ReplicaId = replyValidation.Version.ReplicaId };
                _meta.lastChanges[r.Id] = version;
                return version;
            }
            catch (Exception)
            {
                Console.WriteLine("Storage with ID " + storage.storageNode.serverId + " disconnected");
                _clients.Remove(storageHash);
                return write(r);
            }
        }

        public virtual DIDAWorker.DIDAVersion updateIfValueIs(DIDAWorker.DIDAUpdateIfRequest r)
        {
            if (!_meta.timeStamp.ContainsKey(r.Id))
                CreateTimeStampKey(r.Id);
            int storageHash = LocateStorage(r.Id);
            Client storage = _clients[storageHash];
            try
            {
                if(r.Newvalue == null|| r.Oldvalue == null)
                    return new DIDAWorker.DIDAVersion {VersionNumber = -1, ReplicaId = -1};

                sendUpdateValidationReply reply = storage.client.updateIfValueIs(new DIDAUpdateIfRequest { Id = r.Id, Newvalue = r.Newvalue, Oldvalue = r.Oldvalue });
                UpdateMetaTimeStamp(reply.Tmv, r.Id);
                DIDAWorker.DIDAVersion version = new DIDAWorker.DIDAVersion { VersionNumber = reply.Version.VersionNumber, ReplicaId = reply.Version.ReplicaId };
                if(version.VersionNumber != -1)
                    _meta.lastChanges[r.Id] = version;
                return version;
            }
            catch (Exception e)
            {
                Console.WriteLine("Storage with ID " + storage.storageNode.serverId + " disconnected");
                _clients.Remove(storageHash);
                return updateIfValueIs(r);
            }
        }

        private void UpdateMetaTimeStamp(TimeStampValue tmv,string key)
        {
            List<int> buffer = new List<int>();
            foreach(int i in tmv.NumberUpdates)
            {
                buffer.Add(i);
            }
            _meta.timeStamp[key] = buffer.ToArray();
        }

        private bool CompareTimeStamps(RepeatedField<int> timeStampValueSP, RepeatedField<int> timeStampValueStorage)
        {
            for(int i = 0; i < timeStampValueSP.Count; i++)
            {
                if (timeStampValueSP[i] > timeStampValueStorage[i])
                    return false;
            }
            return true;
        }

        private sendUpdateRequestReq CreateTimeStampValueRepeated(string key, sendUpdateRequestReq request)
        {
            int[] timeStampValue = new int[allClients];
            _meta.timeStamp[key].CopyTo(timeStampValue,0);

            List<int> buffer = new List<int>();
            foreach (int t in timeStampValue)
            {
                buffer.Add(t);
            }
            request.Tmv = new TimeStampValue();
            request.Tmv.NumberUpdates.Add(buffer);
            return request;
        }

        private void CreateTimeStampKey(string key)
        {
            _meta.timeStamp.Add(key, new int[allClients]);
        }

        public void Update(MetaRecord meta)
        {
            _meta = meta;
        }

        internal List<DIDAStorageNode> GetAliveClients()
        {
            List<DIDAStorageNode> buffer = new List<DIDAStorageNode>();
            foreach (Client c in _clients.Values)
            {
                buffer.Add(c.storageNode);
            }

            return buffer;
        }
    }
    
}
