using System;
using System.Collections.Generic;
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
        private string hostname;

        public SchedulerAsServer(Form1 guiWindow, string serverHostname, int serverPort,
                            string clientHostname)
        {
            this.hostname = clientHostname;
            this.guiWindow = guiWindow;
            // setup the client side

            AppContext.SetSwitch(
                "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            channel = GrpcChannel.ForAddress("http://" + serverHostname + ":" + serverPort.ToString());

            client = new DIDASchedulerService.DIDASchedulerServiceClient(channel);
        }
        
        public Boolean SendAppData(string appFilePath, string input)
        {
            SendScriptRequest request = new SendScriptRequest
            {
                Input = input,
            };
            foreach (string line in System.IO.File.ReadLines(@appFilePath))
            {
                request.App.Add(line);
            }


            

            SendScriptReply reply = client.SendScript(request);
            return reply.Ack;
        }
    }

    class PuppetMasterLogic
    {

        private SchedulerAsServer scheduler;

        public PuppetMasterLogic() {}

        public void CreateChannelWithScheduler(Form1 guiWindow, string serverHostname, int serverPort, string clientHostname)
        {
            scheduler = new SchedulerAsServer(guiWindow, serverHostname, serverPort, clientHostname);
        }


        public void SendAppDataToScheduler(string appFilePath, string input)
        {
            scheduler.SendAppData(appFilePath, input);
        }

    }
}
