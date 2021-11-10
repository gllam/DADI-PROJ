﻿using DIDAWorker;
using System;
using Grpc.Net.Client;
using System.Collections.Generic;
using Google.Protobuf.Collections;

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

        DIDAMetaRecord _meta;

        Dictionary<string, int[]> timestamp = new Dictionary<string, int[]>();

        //TODO add timestamp list<int>[nº storages]


        public StorageProxy(DIDAStorageNode[] storageNodes, DIDAMetaRecord metaRecord)
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            int k = 0;
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
            int storage = LocateStorage(r.Id);
            try
            {
                DIDARecordReply res = _clients[storage].client.read(new DIDAReadRequest { Id = r.Id, Version = new DIDAVersion { VersionNumber = r.Version.VersionNumber, ReplicaId = r.Version.ReplicaId } });
                if (!timestamp.ContainsKey(r.Id))
                    CreateTimeStampKey(r.Id);
                return new DIDAWorker.DIDARecordReply { Id = res.Id, Val = res.Val, Version = { VersionNumber = res.Version.VersionNumber, ReplicaId = res.Version.ReplicaId } };
            }
            catch (Exception)
            {
                Console.WriteLine("Storage with ID " + _clients[storage].storageNode.serverId + " disconnected");
                _clients.Remove(storage);
                return read(r);
            }
        }

        public virtual DIDAWorker.DIDAVersion write(DIDAWorker.DIDAWriteRequest r)
        {
            if (!timestamp.ContainsKey(r.Id))
                CreateTimeStampKey(r.Id);
            int storageHash = LocateStorage(r.Id);
            Client storage = _clients[storageHash];
            try
            {
                sendUpdateRequestReq request = new sendUpdateRequestReq { Key = r.Id, Value = r.Val };
                request = CreateTimeStampValueRepeated(r.Id, request);
                TimeStampValue reply = storage.client.sendUpdateRequest(request);
                sendUpdateValidationReply replyValidation = storage.client.sendUpdateValidation(new sendUpdateAck { Success = CompareTimeStamps(request.Tmv.NumberUpdates, reply.NumberUpdates) });
                if(replyValidation.Version.VersionNumber == -1) //while nao é necessario porque a mensagem eventualmente vai chegar
                    replyValidation = storage.client.sendUpdateValidation(new sendUpdateAck { Success = CompareTimeStamps(request.Tmv.NumberUpdates, replyValidation.Tmv.NumberUpdates) });
                return new DIDAWorker.DIDAVersion { VersionNumber = replyValidation.Version.VersionNumber, ReplicaId = replyValidation.Version.ReplicaId };
            }
            catch (Exception)
            {
                Console.WriteLine("Storage with ID " + storage.storageNode.serverId + " disconnected");
                _clients.Remove(storageHash);
                return write(r);
            }
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
            int[] timeStampValue = timestamp[key];
            foreach (int t in timeStampValue)
            {
                request.Tmv.NumberUpdates.Add(t);
            }
            return request;
        }

        public virtual DIDAWorker.DIDAVersion updateIfValueIs(DIDAWorker.DIDAUpdateIfRequest r)
        {
            int storage = LocateStorage(r.Id);
            try
            {
                DIDAVersion res = _clients[storage].client.updateIfValueIs(new DIDAUpdateIfRequest { Id = r.Id, Newvalue = r.Newvalue, Oldvalue = r.Oldvalue });
                if (!timestamp.ContainsKey(r.Id))
                    CreateTimeStampKey(r.Id);
                return new DIDAWorker.DIDAVersion { VersionNumber = res.VersionNumber, ReplicaId = res.ReplicaId };
            }
            catch (Exception)
            {
                Console.WriteLine("Storage with ID " + _clients[storage].storageNode.serverId + " disconnected");
                _clients.Remove(storage);
                return updateIfValueIs(r);
            }
        }

        private void CreateTimeStampKey(string key)
        {
            timestamp.Add(key, new int[_clients.Count]);
        }

        public void Update(DIDAMetaRecord meta)
        {
            _meta = meta;
        }
    }
    
}
