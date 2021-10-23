using System;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;

namespace Process_Creation_Service
{
    class Program
    {
        public class ServerService : DIDAProccessCreatorService.DIDAProccessCreatorServiceBase
        {
            private GrpcChannel channel;

            public ServerService()
            {
            }

            public override Task<CreateProccessInstanceReply> CreateProccessInstance(
                CreateProccessInstanceRequest request, ServerCallContext context)
            {
                /*Console.WriteLine("Deadline: " + context.Deadline);
                Console.WriteLine("Host: " + context.Host);
                Console.WriteLine("Method: " + context.Method);
                Console.WriteLine("Peer: " + context.Peer);*/
                //return Task.FromResult(Reg(request)); This is used to make the calls assynchronous
                CreateProccessInstanceReply reply = new CreateProccessInstanceReply
                {
                    Ack = true
                };
                return Task.FromResult(reply);
                
            }

        }
        public static void Main(string[] args)
        {
            const int port = 10000;
            const string hostname = "localhost";
            string startupMessage;
            ServerPort serverPort;

            serverPort = new ServerPort(hostname, port, ServerCredentials.Insecure);
            startupMessage = "Insecure PCS server listening on port " + port;

            Server server = new Server
            {
                Services = { DIDAProccessCreatorService.BindService(new ServerService()) },
                Ports = { serverPort }
            };

            server.Start();

            Console.WriteLine(startupMessage);
            //Configuring HTTP for client connections in Register method
            AppContext.SetSwitch(
  "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            while (true) ;
        }
    }
}
