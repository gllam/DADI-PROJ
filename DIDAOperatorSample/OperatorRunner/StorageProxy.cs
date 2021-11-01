using DIDAWorker;
using System;
using Grpc.Net.Client;
using System.Collections.Generic;


namespace Worker
{

    public struct Client
    {
        public string id;
        public DIDAStorageService.DIDAStorageServiceClient client;
    }

    public class StorageProxy : IDIDAStorage
    {
        // dictionary with storage gRPC client objects for all storage nodes DIDAStorageService.DIDAStorageServiceClient
        //Dictionary<string, DIDAStorageService.DIDAStorageServiceClient> _clients = new Dictionary<string, DIDAStorageService.DIDAStorageServiceClient>();

        Dictionary<int, Client> _clients = new Dictionary<int, Client>();

        // dictionary with storage gRPC channel objects for all storage nodes
        //Dictionary<string, GrpcChannel> _channels = new Dictionary<string, GrpcChannel>();

        // metarecord for the request that this storage proxy is handling
        DIDAMetaRecord _meta;


        public StorageProxy(DIDAStorageNode[] storageNodes, DIDAMetaRecord metaRecord)
        { //hash all server id's?
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            foreach (DIDAStorageNode n in storageNodes)
            {
                //_channels[n.serverId] = GrpcChannel.ForAddress("http://" + n.host + ":" + n.port);
                //_clients[n.serverId] = new DIDAStorageService.DIDAStorageServiceClient(channel);
                GrpcChannel channel = GrpcChannel.ForAddress("http://" + n.host + ":" + n.port);
                _clients[n.serverId.GetHashCode()] = new Client { id = n.serverId, client = new DIDAStorageService.DIDAStorageServiceClient(channel) };
            }
            _meta = metaRecord;
        }

        //do hash function and key is hash

        // THE FOLLOWING 3 METHODS ARE THE ESSENCE OF A STORAGE PROXY
        // IN THIS EXAMPLE THEY ARE JUST CALLING THE STORAGE 
        // IN THE COMLPETE IMPLEMENTATION THEY NEED TO:
        // 1) LOCATE THE RIGHT STORAGE SERVER
        // 2) DEAL WITH FAILED STORAGE SERVERS
        // 3) CHECK IN THE METARECORD WHICH ARE THE PREVIOUSLY READ VERSIONS OF DATA ???
        // 4) RECORD ACCESSED DATA INTO THE METARECORD ???

        private DIDAStorageService.DIDAStorageServiceClient LocateStorage(string id)
        { // (2)!! 
            int hash = id.GetHashCode();
            int closer = 0;
            foreach(int i in _clients.Keys)
            {
                if (Math.Abs(hash - i) < Math.Abs(hash - closer) || closer == 0)
                {
                    closer = i;

                }
            }
            return _clients[closer].client;
        }

        public virtual DIDAWorker.DIDARecordReply read(DIDAWorker.DIDAReadRequest r)
        {
            var res = LocateStorage(r.Id).read(new DIDAReadRequest { Id = r.Id, Version = new DIDAVersion { VersionNumber = r.Version.VersionNumber, ReplicaId = r.Version.ReplicaId } });
            return new DIDAWorker.DIDARecordReply { Id = res.Id, Val = res.Val, Version = { VersionNumber = res.Version.VersionNumber, ReplicaId = res.Version.ReplicaId } };
        }

        public virtual DIDAWorker.DIDAVersion write(DIDAWorker.DIDAWriteRequest r)
        {
            var res = LocateStorage(r.Id).write(new DIDAWriteRequest { Id = r.Id, Val = r.Val });
            return new DIDAWorker.DIDAVersion { VersionNumber = res.VersionNumber, ReplicaId = res.ReplicaId };
        }

        public virtual DIDAWorker.DIDAVersion updateIfValueIs(DIDAWorker.DIDAUpdateIfRequest r)
        {
            var res = LocateStorage(r.Id).updateIfValueIs(new DIDAUpdateIfRequest { Id = r.Id, Newvalue = r.Newvalue, Oldvalue = r.Oldvalue });
            return new DIDAWorker.DIDAVersion { VersionNumber = res.VersionNumber, ReplicaId = res.ReplicaId };
        }
    }
    
}
