using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DIDAStorage;
using Grpc.Core;

namespace DIDAStorageUI {
    class StorageService : DIDAStorageService.DIDAStorageServiceBase {

        Dictionary<string, List<DIDARecord>> data = new Dictionary<string, List<DIDARecord>>();
        int maxVersions = 5; //dummy
        readonly int replicaid;

        public StorageService(int replicaid)
        {
            this.replicaid = replicaid;
        }

        public override Task<DIDARecordReply> read(DIDAReadRequest request, ServerCallContext context) {
            return Task.FromResult<DIDARecordReply>(ReadData(request));
        }

        private DIDARecordReply ReadData(DIDAReadRequest request) {
            DIDARecordReply reply = new DIDARecordReply
            {
                Id = request.Id,
                Val = null,
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

        public override Task<DIDAVersion> updateIfValueIs(DIDAUpdateIfRequest request, ServerCallContext context) {
            return Task.FromResult<DIDAVersion>(UpdateData(request));
        }

        private DIDAVersion UpdateData(DIDAUpdateIfRequest request) {
            return new DIDAVersion { ReplicaId = -1, VersionNumber = -1 };
        }

        public override Task<DIDAVersion> write(DIDAWriteRequest request, ServerCallContext context) {
            return Task.FromResult<DIDAVersion>(WriteData(request));
        }

        private DIDAVersion WriteData(DIDAWriteRequest request) {
            DIDARecord newRecord = new DIDARecord();
            lock (this)
            {
                newRecord.id = request.Id;
                newRecord.val = request.Val;
                newRecord.version = new DIDAStorage.DIDAVersion
                {
                    replicaId = replicaid,
                    versionNumber = data[request.Id][data[request.Id].Count - 1].version.versionNumber + 1
                };
                data[request.Id].Add(newRecord);
                if (data[request.Id].Count > maxVersions) data[request.Id].RemoveAt(0);
            }
            return new DIDAVersion { ReplicaId = newRecord.version.replicaId, VersionNumber = newRecord.version.versionNumber };
        }
    }

    class Program {
        static void Main(string[] args) {
            int Port = 2001;
            int replicaid = 1;
            Server server = new Server
            {
                Services = { DIDAStorageService.BindService(new StorageService(replicaid)) },
                Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) }
            };
            server.Start();
            Console.ReadKey();
            server.ShutdownAsync().Wait();
        }
    }
}
