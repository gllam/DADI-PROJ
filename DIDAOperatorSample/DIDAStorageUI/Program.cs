using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DIDAStorage;
using Grpc.Core;

namespace DIDAStorageUI
{
    class StorageService : DIDAStorageService.DIDAStorageServiceBase
    {

        Dictionary<string, List<DIDARecord>> data = new Dictionary<string, List<DIDARecord>>();
        int maxVersions = 5; //dummy
        readonly int replicaid;
        string name;
        int gossipDelay;

        //TODO updateif ; S2S.proto(pm cliente) ; data consistency ; fault tolerance ; gossip

        public StorageService(int replicaid, int gossipDelay, string name)
        {
            this.gossipDelay = gossipDelay;
            this.replicaid = replicaid;
            this.name = name;
        }
        override
        public string ToString()
        {
            return "TODO " + this.name;
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
            if (request.Version == null) reply.Version.VersionNumber = 0;
            lock (this)
            {
                if (data.ContainsKey(request.Id))
                {
                    foreach (DIDARecord record in data[request.Id])
                    {
                        if (record.version.versionNumber == reply.Version.VersionNumber && request.Version != null)
                        {
                            reply.Val = record.val;
                            break;
                        }
                        if (record.version.versionNumber > reply.Version.VersionNumber && request.Version == null)
                        {
                            reply.Val = record.val;
                            reply.Version = new DIDAVersion
                            {
                                VersionNumber = record.version.versionNumber,
                                ReplicaId = record.version.replicaId
                            };
                        }
                    }
                }
            }
            return reply;
        }

        public override Task<DIDAVersion> updateIfValueIs(DIDAUpdateIfRequest request, ServerCallContext context)
        {
            return Task.FromResult<DIDAVersion>(UpdateData(request));
        }

        private DIDAVersion UpdateData(DIDAUpdateIfRequest request)
        {
            lock (this)
            {
                if (data.ContainsKey(request.Id))
                {
                    if (data[request.Id][data[request.Id].Count - 1].val == request.Oldvalue)
                    {
                        return WriteData(new DIDAWriteRequest
                        {
                            Id = request.Id,
                            Val = request.Newvalue
                        });
                    }
                }
                return new DIDAVersion { ReplicaId = -1, VersionNumber = -1 }; //null version
            }
        }

        public override Task<DIDAVersion> write(DIDAWriteRequest request, ServerCallContext context)
        {
            return Task.FromResult<DIDAVersion>(WriteData(request));
        }

        private DIDAVersion WriteData(DIDAWriteRequest request)
        {
            DIDARecord newRecord = new DIDARecord();
            lock (this)
            {
                newRecord.id = request.Id;
                newRecord.val = request.Val;
                newRecord.version = new DIDAStorage.DIDAVersion
                {
                    replicaId = replicaid
                };
                if (data.ContainsKey(request.Id))
                {
                    newRecord.version.versionNumber = data[request.Id][data[request.Id].Count - 1].version.versionNumber + 1;
                }
                else
                {
                    newRecord.version.versionNumber = 1;
                    data.Add(request.Id, new List<DIDARecord>());
                }
                data[request.Id].Add(newRecord);
                if (data[request.Id].Count > maxVersions) data[request.Id].RemoveAt(0);
                Console.WriteLine("Write successful -> " + newRecord.id + ":" + newRecord.val);
            }
            return new DIDAVersion { ReplicaId = newRecord.version.replicaId, VersionNumber = newRecord.version.versionNumber };
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

            Console.WriteLine("Insecure Storage server '" + storageName + "' | hostname: " + host + " | port " + port);
            Server server = new Server
            {
                Services = { DIDAStorageService.BindService(new StorageService(replicaid, gossipDelay, storageName)) },
                Ports = { new ServerPort(host, port, ServerCredentials.Insecure) }
            };
            server.Start();
            Console.ReadKey();
            server.ShutdownAsync().Wait();
        }
    }
}
