using DIDAWorker;
using System;
using Grpc.Net.Client;
using System.Collections.Generic;


namespace Worker
{
    public class StorageProxy : IDIDAStorage
    {
        // dictionary with storage gRPC client objects for all storage nodes DIDAStorageService.DIDAStorageServiceClient
        Dictionary<string, DIDAStorageService.DIDAStorageServiceClient> _clients = new Dictionary<string, DIDAStorageService.DIDAStorageServiceClient>();

        // dictionary with storage gRPC channel objects for all storage nodes
        Dictionary<string, GrpcChannel> _channels = new Dictionary<string, GrpcChannel>();

        // metarecord for the request that this storage proxy is handling
        DIDAMetaRecord _meta;


        public StorageProxy(DIDAStorageNode[] storageNodes, DIDAMetaRecord metaRecord)
        { //hash all server id's?
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            foreach (DIDAStorageNode n in storageNodes)
            {
                _channels[n.serverId] = GrpcChannel.ForAddress("http://" + n.host + ":" + n.port);
                _clients[n.serverId] = new DIDAStorageService.DIDAStorageServiceClient(_channels[n.serverId]);
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

        public virtual DIDARecordReply read(DIDAWorker.DIDAReadRequest r)
        {
            return _clients["s1"].read(new DIDAReadRequest { Id = r.Id, Version = new DIDAVersion { VersionNumber = r.Version.VersionNumber, ReplicaId = r.Version.ReplicaId } });
        }

        public virtual DIDAVersion write(DIDAWorker.DIDAWriteRequest r)
        {
            return _clients["s1"].write(new DIDAWriteRequest { Id = r.Id, Val = r.Val });
        }

        public virtual DIDAVersion updateIfValueIs(DIDAWorker.DIDAUpdateIfRequest r)
        {
            return _clients["s1"].updateIfValueIs(new DIDAUpdateIfRequest { Id = r.Id, Newvalue = r.Newvalue, Oldvalue = r.Oldvalue });
        }
    }
}
