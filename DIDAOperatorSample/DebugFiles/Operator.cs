using System;
using DIDAStorage;
using DIDAWorker;
using DIDAStorageClient;
using Grpc.Core;
using Grpc.Net.Client;
using System.Collections.Generic;

namespace DIDAOperator {

    public class UpdateAndChainOperator : IDIDAOperator {
        Dictionary<string, DIDAStorageService.DIDAStorageServiceClient> _storageServers =
            new Dictionary<string, DIDAStorageService.DIDAStorageServiceClient>();
        Dictionary<string, GrpcChannel> _storageChannels =
             new Dictionary<string, GrpcChannel>();
        delLocateStorageId _locationFunction;

        public UpdateAndChainOperator() {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        }

        // this operator increments the storage record identified in the metadata record every time it is called.
        string IDIDAOperator.ProcessRecord(DIDAMetaRecord meta, string input, string previousOperatorOutput) {
            string storageServer = _locationFunction(meta.id.ToString(), OperationType.ReadOp).serverId;
            var val = _storageServers[storageServer].updateIfValueIs(new DIDAUpdateIfRequest { Id = input, Newvalue = "success", Oldvalue = previousOperatorOutput });
            val = _storageServers[storageServer].updateIfValueIs(new DIDAUpdateIfRequest { Id = input, Newvalue = "failure", Oldvalue = previousOperatorOutput });

            Console.WriteLine("updated record:" + input );
            return "end";
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

    public class AddOperator : IDIDAOperator {
        Dictionary<string, DIDAStorageService.DIDAStorageServiceClient> _storageServers =
            new Dictionary<string, DIDAStorageService.DIDAStorageServiceClient>();
        Dictionary<string, GrpcChannel> _storageChannels =
             new Dictionary<string, GrpcChannel>();
        delLocateStorageId _locationFunction;

        public AddOperator() {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        }

        // this operator increments the storage record identified in the metadata record every time it is called.
        string IDIDAOperator.ProcessRecord(DIDAMetaRecord meta, string input, string previousOperatorOutput) {
            Console.WriteLine("input string was: " + input);
            Console.Write("reading data record: " + input + " with value: ");
            string storageServer = _locationFunction(input, OperationType.ReadOp).serverId;
            var val = _storageServers[storageServer].read(new DIDAReadRequest { Id = input, Version = new DIDAStorageClient.DIDAVersion { VersionNumber = -1, ReplicaId = -1 } });
            string storedString = val.Val;
            Console.WriteLine(storedString);
            int requestCounter;
            string output;
            try {
                requestCounter = Int32.Parse(storedString);
                requestCounter++;
                output = requestCounter.ToString();
            } catch (Exception e) {
                output = "int_conversion_failed";
                Console.WriteLine(" operator expecting int but got chars: " + e.Message);
            }


            int oneAhead = Int32.Parse(input);
            oneAhead++;
            Console.Write("reading data record: " + oneAhead + " with value: ");
            storageServer = _locationFunction(oneAhead.ToString(), OperationType.ReadOp).serverId;
            val = _storageServers[storageServer].read(new DIDAReadRequest { Id = oneAhead.ToString(), Version = new DIDAStorageClient.DIDAVersion { VersionNumber = -1, ReplicaId = -1 } });
            storedString = val.Val;
            Console.WriteLine(storedString);


            storageServer = _locationFunction(meta.id.ToString(), OperationType.WriteOp).serverId;
            _storageServers[storageServer].write(new DIDAWriteRequest { Id = input, Val = output });
            Console.WriteLine("writing data record:" + input + " with new value: " + output);
            return output;
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

    public class IncrementOperator : IDIDAOperator {
        Dictionary<string, DIDAStorageService.DIDAStorageServiceClient> _storageServers =
            new Dictionary<string, DIDAStorageService.DIDAStorageServiceClient>();
        Dictionary<string, GrpcChannel> _storageChannels =
             new Dictionary<string, GrpcChannel>();
        delLocateStorageId _locationFunction;

        public IncrementOperator() {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        }
        
        // this operator increments the storage record identified in the metadata record every time it is called.
        string IDIDAOperator.ProcessRecord(DIDAMetaRecord meta, string input, string previousOperatorOutput) {
            Console.WriteLine("input string was: " + input);
            Console.Write("reading data record: " + meta.id + " with value: ");
            string storageServer = _locationFunction(meta.id.ToString(), OperationType.ReadOp).serverId;
            var val = _storageServers[storageServer].read(new DIDAReadRequest { Id = meta.id.ToString(), Version = new DIDAStorageClient.DIDAVersion { VersionNumber = -1, ReplicaId = -1} });
            string storedString = val.Val;
            Console.WriteLine(storedString);
            int requestCounter = Int32.Parse(storedString);

            requestCounter++;
            //requestCounter += Int32.Parse(previousOperatorOutput);
            
            storageServer = _locationFunction(meta.id.ToString(), OperationType.WriteOp).serverId;
            _storageServers[storageServer].write(new DIDAWriteRequest { Id = meta.id.ToString(), Val = requestCounter.ToString() });
            Console.WriteLine("writing data record:" + meta.id + " with new value: " + requestCounter.ToString());
            return requestCounter.ToString();
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
