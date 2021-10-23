using System;
using System.Diagnostics;
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
                Console.WriteLine("Creating scheduler");
                /*Console.WriteLine("Deadline: " + context.Deadline);
                Console.WriteLine("Host: " + context.Host);
                Console.WriteLine("Method: " + context.Method);
                Console.WriteLine("Peer: " + context.Peer);*/
                //return Task.FromResult(Reg(request)); This is used to make the calls assynchronous

                return Task.FromResult(CreateProcInstance(request));
                
            }

            private CreateProccessInstanceReply CreateProcInstance(CreateProccessInstanceRequest request)
            {
                int port;
                string hostname;
                string urlRefined;
                /*Process.Start(@"C:\Users\david\Desktop\Aulas\Mestrado\1º ano\PADI\Proj\DADI-PROJ\DIDAOperatorSample\Scheduler2\bin\Debug\netcoreapp3.1\Scheduler.exe");
                Assembly assem = typeof(Scheduler).Assembly;
                Scheduler scheduler = (Scheduler)assem.CreateInstance("SchedulerNamespace.Scheduler");

                urlRefined = request.Url.Split("http://")[1];
                port = Convert.ToInt32(urlRefined.Split(':')[1]);
                hostname = urlRefined.Split(':')[0];
                string[] args = null;
                Scheduler.Main(args);
                scheduler.Initialize(request.ServerId, hostname, port);*/
                urlRefined = request.Url.Split("http://")[1];
                port = Convert.ToInt32(urlRefined.Split(':')[1]);
                hostname = urlRefined.Split(':')[0];
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = @"C:\Users\david\Desktop\Aulas\Mestrado\1º ano\PADI\Proj\DADI-PROJ\DIDAOperatorSample\Scheduler2\bin\Debug\netcoreapp3.1\Scheduler.exe",
                    UseShellExecute = true,
                    CreateNoWindow = false,
                    Arguments = request.ServerId + " " + hostname + " " + port
                };
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
