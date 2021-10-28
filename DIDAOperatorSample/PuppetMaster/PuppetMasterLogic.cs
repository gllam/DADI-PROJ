using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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

        internal Boolean SendCreateSchedulerRequest(string[] scheduler, string[][] workers)
        {
            CreateSchedulerInstanceRequest request = new CreateSchedulerInstanceRequest
            {
                MyData = new ProccessData
                {
                    ServerId = scheduler[1],
                    Url = scheduler[2]
                }
            };
            foreach (string[] worker in workers)
            {
                request.DependenciesData.Add(new ProccessData
                { 
                    ServerId = worker[1] ,
                    Url = worker[2]
                });
            }

            CreateProccessInstanceReply reply = client.CreateSchedulerInstance(request);
            return reply.Ack;
        }

        internal Boolean SendCreateWorkerRequest(string[] worker, string[][] storages)
        {
            CreateWorkerInstanceRequest request = new CreateWorkerInstanceRequest
            {
                MyData = new ProccessData
                {
                    ServerId = worker[1],
                    Url = worker[2],
                },
                GossipDelay = worker[3] 
            };
            foreach (string[] storage in storages)
            {
                request.DependenciesData.Add(new ProccessData
                {
                    ServerId = storage[1],
                    Url = storage[2]
                });
            }

            CreateProccessInstanceReply reply = client.CreateWorkerInstance(request);
            return reply.Ack;
        }

        internal Boolean SendCreateStorageRequest(string[] storage)
        {
            CreateStorageInstanceRequest request = new CreateStorageInstanceRequest
            {
                MyData = new ProccessData
                {
                    ServerId = storage[1],
                    Url = storage[2],
                },
                GossipDelay = storage[3]
            };

            CreateProccessInstanceReply reply = client.CreateStorageInstance(request);
            return reply.Ack;
        }
    }

    class PuppetMasterLogic
    {

        private SchedulerAsServer scheduler;
        private ProcessCreatorAsServer pcs;

        public PuppetMasterLogic(Form1 guiWindow) {
            pcs = new ProcessCreatorAsServer(guiWindow);
        }
        
        public void CreateChannelWithScheduler(string serverId, string url)
        {
            string urlRefined = url.Split("http://")[1];
            string port = urlRefined.Split(':')[1];
            string hostname = urlRefined.Split(':')[0];
            scheduler = new SchedulerAsServer(serverId,hostname, port);
        }

        public Boolean SendAppDataToScheduler(string[] buffer)
        {
            while(scheduler == null) {
                Task.Delay(100);
            }
            return scheduler.SendAppData(buffer);

        }

        internal void CreateAllConfigEvents(string[] scheduler, string[][] workers, string[][] storages)
        {
            //Create Scheduler
            pcs.SendCreateSchedulerRequest(scheduler, workers);
            this.CreateChannelWithScheduler(scheduler[1],scheduler[2]);
            //Create workers
            foreach(string[] worker in workers)
            {
                pcs.SendCreateWorkerRequest(worker, storages);
            }

            //Create Storages
            foreach (string[] storage in storages)
            {
                pcs.SendCreateStorageRequest(storage);
            }

        }

        /*public void SendCreateProccessInstanceRequest(string serverId, string url)
        {
            pcs.SendCreateProccessInstanceRequest(serverId, url);
        }*/
    }
}
