using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;
using SchedulerNamespace;

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
                Console.WriteLine("Creating " + request.Type);
                return Task.FromResult(CreateProcInstance(request));
                
            }

            private CreateProccessInstanceReply CreateProcInstance(CreateProccessInstanceRequest request)
            {
                int port;
                string hostname;
                string urlRefined;
                urlRefined = request.Url.Split("http://")[1];
                port = Convert.ToInt32(urlRefined.Split(':')[1]);
                hostname = urlRefined.Split(':')[0];
                string workingDirectory = Path.GetFullPath(request.Type + ".exe");
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = workingDirectory,
                    UseShellExecute = true,
                    CreateNoWindow = false,
                };
                if(request.Type.Equals("scheduler"))
                { psi.Arguments = request.ServerId + " " + hostname + " " + port; }
                else { psi.Arguments = request.ServerId + " " + hostname + " " + port + " " + request.GossipDelay; }

                Process.Start(psi);

                return new CreateProccessInstanceReply
                {
                    Ack = true
                };
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
