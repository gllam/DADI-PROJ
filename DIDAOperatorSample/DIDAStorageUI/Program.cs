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
        int gossipDelay;

        //TODO updateif ; S2S.proto(pm cliente) ; data consistency ; fault tolerance 

        public StorageService(int replicaid, int gossipDelay)
        {
            this.gossipDelay = gossipDelay;
            this.replicaid = replicaid;
        }

        public override Task<DIDARecordReply> read(DIDAReadRequest request, ServerCallContext context)
        {
            return Task.FromResult<DIDARecordReply>(ReadData(request));
        }

        private DIDARecordReply ReadData(DIDAReadRequest request)
        {
            try
            {
                DIDARecordReply reply = new DIDARecordReply
                {
                    Id = request.Id,
                    Version = request.Version
                };
                if (request.Version == null) reply.Version.VersionNumber = 0;
                lock (this)
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
                return reply;
            }
            catch (Exception e)
            {
                return new DIDARecordReply();
            }
        }

        public override Task<DIDAVersion> updateIfValueIs(DIDAUpdateIfRequest request, ServerCallContext context)
        {
            return Task.FromResult<DIDAVersion>(UpdateData(request));
        }

        private DIDAVersion UpdateData(DIDAUpdateIfRequest request)
        {
            lock (this)
            {
                if (data[request.Id][data[request.Id].Count - 1].val == request.Oldvalue)
                {
                    return WriteData(new DIDAWriteRequest
                    {
                        Id = request.Id,
                        Val = request.Newvalue
                    });
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
                try
                {
                    newRecord.version.versionNumber = data[request.Id][data[request.Id].Count - 1].version.versionNumber + 1;
                }
                catch (Exception e)
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
            int port = Convert.ToInt32(args[2]);
            string host = args[1];
            int replicaid = Convert.ToInt32(args[0]);
            int gossipDelay = Convert.ToInt32(args[3]);

            Console.WriteLine("Insecure Storage server '" + replicaid + "' | hostname: " + host + " | port " + port);
            Server server = new Server
            {
                Services = { DIDAStorageService.BindService(new StorageService(replicaid,gossipDelay)) },
                Ports = { new ServerPort(host, port, ServerCredentials.Insecure) }
            };
            server.Start();
            Console.ReadKey();
            server.ShutdownAsync().Wait();
        }
    }
}
