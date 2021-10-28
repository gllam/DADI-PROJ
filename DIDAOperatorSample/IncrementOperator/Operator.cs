using System;
using DIDAStorage;
using DIDAWorker;
using DIDAStorageClient;
using Grpc.Core;
using Grpc.Net.Client;
using System.Collections.Generic;

namespace DIDAOperator {
    public class CounterOperator : IDIDAOperator {
        Dictionary<string, DIDAStorageService.DIDAStorageServiceClient> _storageServers =
            new Dictionary<string, DIDAStorageService.DIDAStorageServiceClient>();
        Dictionary<string, GrpcChannel> _storageChannels =
             new Dictionary<string, GrpcChannel>();
        delLocateStorageId _locationFunction;

        public CounterOperator() {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        }
        
        // this operator increments the storage record identified in the metadata record every time it is called.
        string IDIDAOperator.ProcessRecord(DIDAMetaRecord meta, string input, string previousOperatorOutput) {
            try
            {
                Console.WriteLine("input string was: " + input);
                Console.Write("reading data record: " + meta.id + " with value: ");
                string storageServer = _locationFunction(meta.id.ToString(), OperationType.ReadOp).serverId;
                var val = _storageServers[storageServer].read(new DIDAReadRequest { Id = meta.id.ToString(), Version = new DIDAStorageClient.DIDAVersion { VersionNumber = -1, ReplicaId = -1 } });
                int requestCounter = 0;
                Console.WriteLine(val);
                if (val.Equals(null))
                {
                    string storedString = val.Val;
                    Console.WriteLine(storedString);
                    requestCounter = Int32.Parse(storedString);
                }
                //Console.WriteLine("sss");
                requestCounter++;
                storageServer = _locationFunction(meta.id.ToString(), OperationType.WriteOp).serverId;
                //Console.WriteLine("sssyyyy");
                _storageServers[storageServer].write(new DIDAWriteRequest { Id = meta.id.ToString(), Val = requestCounter.ToString() });
                Console.WriteLine("writing data record: " + meta.id + "with new value: " + requestCounter.ToString());
                return requestCounter.ToString();
            } catch (Exception e)
            {
                Console.WriteLine("error " + e);
                return "lol";
            }
        }

        void IDIDAOperator.ConfigureStorage(DIDAStorageNode[] storageReplicas, delLocateStorageId locationFunction) {
            DIDAStorageService.DIDAStorageServiceClient client;
            GrpcChannel channel;

            _locationFunction = locationFunction;

            foreach (DIDAStorageNode n in storageReplicas) {
                channel = GrpcChannel.ForAddress("http://" + n.host + ":" + n.port + "/");
                client = new DIDAStorageService.DIDAStorageServiceClient(channel);
                _storageServers.Add(n.serverId, client);
                _storageChannels.Add(n.serverId, channel);
                }
        }
    }
}
