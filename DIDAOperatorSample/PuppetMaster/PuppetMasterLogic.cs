using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;

namespace PuppetMaster
{
    public class SchedulerAsServer
    {
        private readonly GrpcChannel channel;
        private readonly DIDASchedulerService.DIDASchedulerServiceClient client;
        private readonly string serverId;

        public SchedulerAsServer(string serverId, string hostname, string port)
        {
            this.serverId = serverId;
            // setup the client side

            AppContext.SetSwitch(
                "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            channel = GrpcChannel.ForAddress("http://" + hostname + ":" + port);

            client = new DIDASchedulerService.DIDASchedulerServiceClient(channel);
        }
        
        public Boolean SendAppData(string[] data)
        {
            SendAppDataRequest request = new SendAppDataRequest
            {
                Input = data[1],
            };
            //need to check where we put the input files
            foreach (string line in System.IO.File.ReadLines(Path.GetFullPath(data[2])))
            {
                request.App.Add(line);
            }

            SendAppDataReply reply = client.SendAppData(request);
            return reply.Ack;
        }
    }

    public class ProcessCreatorAsServer
    {
        private readonly GrpcChannel channel;
        private readonly DIDAProccessCreatorService.DIDAProccessCreatorServiceClient client;
        private readonly Form1 guiWindow;

        public ProcessCreatorAsServer(Form1 guiWindow)
        {
            this.guiWindow = guiWindow;
            // setup the client side

            AppContext.SetSwitch(
                "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            channel = GrpcChannel.ForAddress("http://localhost:10000");

            client = new DIDAProccessCreatorService.DIDAProccessCreatorServiceClient(channel);
        }

        internal Boolean SendCreateSchedulerRequest(int replicaId, string[] scheduler, string[][] workers, List<String> workersMap)
        {
            CreateSchedulerInstanceRequest request = new CreateSchedulerInstanceRequest
            {
                MyData = new ProccessData
                {
                    ServerId = replicaId,
                    Url = scheduler[2]
                }
            };
            int workerId;
            foreach (string[] worker in workers)
            {
                workerId = workersMap.BinarySearch(worker[0]);
                request.DependenciesData.Add(new ProccessData
                { 
                    ServerId = workerId ,
                    Url = worker[2]
                });
            }

            CreateProccessInstanceReply reply = client.CreateSchedulerInstance(request);
            return reply.Ack;
        }

        internal Boolean SendCreateWorkerRequest(int replicaId,string[] worker, string[][] storages, List<string> storageMap)
        {
            CreateWorkerInstanceRequest request = new CreateWorkerInstanceRequest
            {
                MyData = new ProccessData
                {
                    ServerId = replicaId,
                    Url = worker[2],
                },
                GossipDelay = worker[3] 
            };
            int storageId;
            foreach (string[] storage in storages)
            {
                storageId = storageMap.BinarySearch(storage[0]);
                request.DependenciesData.Add(new ProccessData
                {
                    ServerId = storageId,
                    Url = storage[2]
                });
            }

            CreateProccessInstanceReply reply = client.CreateWorkerInstance(request);
            return reply.Ack;
        }

        internal Boolean SendCreateStorageRequest(int replicaId,string[] storage, string[][] storages)
        {
            CreateStorageInstanceRequest request = new CreateStorageInstanceRequest
            {
                MyData = new ProccessData
                {
                    ServerId = replicaId,
                    Url = storage[2],
                },
                GossipDelay = storage[3]
            };

            foreach (string[] storageData in storages)
            {
                request.StorageUrl.Add(storageData[2]);
            }

            CreateProccessInstanceReply reply = client.CreateStorageInstance(request);
            return reply.Ack;
        }
    }
    //TODO
    public class StorageAsServer
    {
        private readonly GrpcChannel channel;
        private readonly DIDAStorageService.DIDAStorageServiceClient client;
        private readonly string serverId;

        public StorageAsServer(string serverId, string hostname, string port)
        {
            this.serverId = serverId;
            // setup the client side

            AppContext.SetSwitch(
                "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            channel = GrpcChannel.ForAddress("http://" + hostname + ":" + port);

            client = new DIDAStorageService.DIDAStorageServiceClient(channel);
        }

        public Boolean SendWriteRequest(string key, string value)
        {
            DIDAWriteRequest request = new DIDAWriteRequest
            {
                Id = key,
                Val = value
            };
            DIDAVersion data = client.write(request);
            if (data == null)
                return false;
            return true;
        }
    }


    class PuppetMasterLogic
    {

        private SchedulerAsServer scheduler;
        private ProcessCreatorAsServer pcs;
        private List<StorageAsServer> storagesAsServers = new List<StorageAsServer>();
        private List<string> schedulerMap = new List<string>();
        private List<string> workerMap = new List<string>();
        private List<string> storageMap = new List<string>();

        public PuppetMasterLogic(Form1 guiWindow) {
            pcs = new ProcessCreatorAsServer(guiWindow);
        }
        public void CreateChannelWithServer(string serverId, string url, string type)
        {
            string urlRefined = url.Split("http://")[1];
            string port = urlRefined.Split(':')[1];
            string hostname = urlRefined.Split(':')[0];
            if (type == "scheduler")
                scheduler = new SchedulerAsServer(serverId, hostname, port);
            else if (type == "storage")
            {
                StorageAsServer storage = new StorageAsServer(serverId, hostname, port);
                storagesAsServers.Add(storage);
            }

        }
        internal void ExecuteCommand(string commandLine)
        {
            Thread t;
            string[] buffer = commandLine.Split(' ');
            switch (buffer[0])
            {
                case "client":
                    t = new Thread(new ThreadStart(() => this.SendAppDataToScheduler(buffer)));
                    t.Start();
                    break;
                case "populate":
                    t = new Thread(new ThreadStart(() => this.StartPopulateStoragesOperation(buffer[1])));
                    t.Start();
                    break;

                default:
                    break;

            }

        }
        public Boolean SendAppDataToScheduler(string[] buffer)
        {
            /*while(scheduler == null) {
                Task.Delay(100);
            }*/
            return scheduler.SendAppData(buffer);

        }

        internal void CreateAllConfigEvents(string[] scheduler, string[][] workers, string[][] storages)
        {
            //Create Scheduler
            schedulerMap.Add(scheduler[0]);
            foreach (string[] worker in workers)
            {
                workerMap.Add(worker[0]);
            }
            foreach (string[] storage in storages)
            {
                storageMap.Add(storage[0]);
            }
            int replicaId = schedulerMap.BinarySearch(scheduler[0]);
            pcs.SendCreateSchedulerRequest(replicaId, scheduler, workers, workerMap);
            this.CreateChannelWithServer(scheduler[1], scheduler[2], "scheduler");

            //Create Workers
            foreach (string[] worker in workers)
            {
                replicaId = workerMap.BinarySearch(worker[0]);
                pcs.SendCreateWorkerRequest(replicaId, worker, storages, storageMap);
            }

            //Create Storages
            foreach (string[] storage in storages)
            {
                replicaId = storageMap.BinarySearch(storage[0]);
                pcs.SendCreateStorageRequest(replicaId, storage, storages);
                this.CreateChannelWithServer(storage[1], storage[2], "storage");
            }

        }

        internal void StartPopulateStoragesOperation(string storageDataFileName)
        {
            foreach (string line in System.IO.File.ReadLines(storageDataFileName))
            {
                string[] data = line.Split(',');
                this.SendDataToStorage(data[0],data[1]);
            }
        }

        private void SendDataToStorage(string key, string value)
        {
            //Calculate which storage needs to receive the Data TODO
            storagesAsServers[0].SendWriteRequest(key, value);

        }

        /*public void SendCreateProccessInstanceRequest(string serverId, string url)
        {
            pcs.SendCreateProccessInstanceRequest(serverId, url);
        }*/
    }
}
