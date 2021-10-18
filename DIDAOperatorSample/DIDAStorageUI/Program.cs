using System;
using System.Threading.Tasks;
using DIDAStorage;
using Grpc.Core;

namespace DIDAStorageUI {
    // dummy storage
    class StorageService : DIDAStorageService.DIDAStorageServiceBase {
        public override Task<DIDARecordReply> read(DIDAReadRequest request, ServerCallContext context) {
            return Task.FromResult<DIDARecordReply>(ReadImpl(request));
        }

        private DIDARecordReply ReadImpl(DIDAReadRequest request) {
            return new DIDARecordReply
            {
                Id = request.Id,
                Val = "1",
                Version = new DIDAVersion { ReplicaId = 1, VersionNumber = 1 }
            };
        }

        public override Task<DIDAVersion> updateIfValueIs(DIDAUpdateIfRequest request, ServerCallContext context) {
            return Task.FromResult<DIDAVersion>(UpdateImpl(request));
        }

        private DIDAVersion UpdateImpl(DIDAUpdateIfRequest request) {
            return new DIDAVersion { ReplicaId = -1, VersionNumber = -1 };
        }

        public override Task<DIDAVersion> write(DIDAWriteRequest request, ServerCallContext context) {
            return Task.FromResult<DIDAVersion>(WriteImpl(request));
        }

        private DIDAVersion WriteImpl(DIDAWriteRequest request) {
            return new DIDAVersion { ReplicaId = -1, VersionNumber = -1 };
        }
    }

    class Program {
        static void Main(string[] args) {
            int Port = 2001;
            Server server = new Server
            {
                Services = { DIDAStorageService.BindService(new StorageService()) },
                Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) }
            };
            server.Start();
            Console.ReadKey();
            server.ShutdownAsync().Wait();
        }
    }
}
