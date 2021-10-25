using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Grpc.Core;
using Grpc.Net.Client;

namespace PuppetMaster
{
    public class SchedulerAsServer
    {
        private readonly GrpcChannel channel;
        private readonly DIDASchedulerService.DIDASchedulerServiceClient client;
        private readonly Form1 guiWindow;
        private readonly string serverId;

        public SchedulerAsServer(Form1 guiWindow, string serverId, string hostname, string port)
        {
            this.guiWindow = guiWindow;
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

        public Boolean SendCreateProccessInstanceRequest(string[] parsedInput)
        {
            CreateProccessInstanceRequest request;
            if (parsedInput[0].Equals("scheduler")) { 
                request = new CreateProccessInstanceRequest
                {
                    Type = parsedInput[0],
                    ServerId = parsedInput[1],
                    Url = parsedInput[2],
                    GossipDelay = 0
                };
            }
            else
            {
                request = new CreateProccessInstanceRequest
                {
                    Type = parsedInput[0],
                    ServerId = parsedInput[1],
                    Url = parsedInput[2],
                    GossipDelay = Convert.ToInt32(parsedInput[3])
                };

            }

            CreateProccessInstanceReply reply = client.CreateProccessInstance(request);
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
        
        public void CreateChannelWithScheduler(Form1 guiWindow, string serverId, string url)
        {
            string urlRefined = url.Split("http://")[1];
            string port = urlRefined.Split(':')[1];
            string hostname = urlRefined.Split(':')[0];
            scheduler = new SchedulerAsServer(guiWindow, serverId,hostname, port);
        }

        public void SendAppDataToScheduler(string[] buffer)
        {
            scheduler.SendAppData(buffer);
        }

        /*public void SendCreateProccessInstanceRequest(string serverId, string url)
        {
            pcs.SendCreateProccessInstanceRequest(serverId, url);
        }*/

        internal void CreateNewConfigEvent(string[] parsedInput) {
            pcs.SendCreateProccessInstanceRequest(parsedInput);
        }
    }
}
