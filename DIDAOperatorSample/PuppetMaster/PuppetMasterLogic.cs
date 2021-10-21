using System;
using System.Collections.Generic;
using System.Text;
using Grpc.Core;
using Grpc.Net.Client;

namespace PuppetMaster
{
    public class SchedulerServer
    {
        private readonly GrpcChannel channel;
        private readonly DIDASchedulerService.DIDASchedulerServiceClient client;
        private Server server;
        private readonly Form1 guiWindow;
        private string nick;
        private string hostname;

        public SchedulerServer(Form1 guiWindow, string serverHostname, int serverPort,
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
        
        public Boolean SendScript(String input)
        {
            SendScriptRequest request = new SendScriptRequest
            {
                Input = input,
            };
            request.App.Add("lol");

            SendScriptReply reply = client.SendScript(request);
            return reply.Ack;
        }
    }

    class PuppetMasterLogic
    {

        private SchedulerServer scheduler;

        public PuppetMasterLogic() {}

        public void createChannelWithScheduler(Form1 guiWindow, string serverHostname, int serverPort, string clientHostname)
        {
            scheduler = new SchedulerServer(guiWindow, serverHostname, serverPort, clientHostname);
        }

        public void SendScript(String input )
        {
            scheduler.SendScript(input);
        }

    }
}
