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

            public override Task<CreateProccessInstanceReply> CreateSchedulerInstance(
                CreateSchedulerInstanceRequest request, ServerCallContext context)
            {
                Console.WriteLine("Creating Scheduler: " + request.MyData.ServerName);
                return Task.FromResult(CreateSchedInstance(request));
                
            }
            private CreateProccessInstanceReply CreateSchedInstance(CreateSchedulerInstanceRequest request)
            {
                int port;
                string hostname;
                string urlRefined;
                urlRefined = request.MyData.Url.Split("http://")[1];
                port = Convert.ToInt32(urlRefined.Split(':')[1]);
                hostname = urlRefined.Split(':')[0];
                string workingDirectory = Path.GetFullPath("scheduler.exe");
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = workingDirectory,
                    UseShellExecute = true,
                    CreateNoWindow = false,
                };
                psi.Arguments = Convert.ToString(request.ServerId) + " " + request.MyData.ServerName + " " + hostname + " " + port;
                foreach(ProccessData data in request.DependenciesData)
                {
                    psi.Arguments = psi.Arguments + " " + data.ServerName + "|" + data.Url;
                }

                Process.Start(psi);
                return new CreateProccessInstanceReply
                {
                    Ack = true
                };
            }

            public override Task<CreateProccessInstanceReply> CreateWorkerInstance(
                                    CreateWorkerInstanceRequest request, ServerCallContext context)
            {
                Console.WriteLine("Creating Worker: " + request.MyData.ServerName);
                return Task.FromResult(CreateWorkInstance(request));

            }

            private CreateProccessInstanceReply CreateWorkInstance(CreateWorkerInstanceRequest request)
            {
                int port;
                string hostname;
                string urlRefined;
                urlRefined = request.MyData.Url.Split("http://")[1];
                port = Convert.ToInt32(urlRefined.Split(':')[1]);
                hostname = urlRefined.Split(':')[0];
                string workingDirectory = Path.GetFullPath("worker.exe");
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = workingDirectory,
                    UseShellExecute = true,
                    CreateNoWindow = false,
                };
                psi.Arguments = Convert.ToString(request.ServerId) + " " + request.MyData.ServerName + " " + hostname + " " + port + " " + request.GossipDelay;
                foreach (ProccessData data in request.DependenciesData)
                {
                    psi.Arguments = psi.Arguments + " " + data.ServerName + "|"+ data.Url;
                }

                Process.Start(psi);
                return new CreateProccessInstanceReply
                {
                    Ack = true
                };
            }

            public override Task<CreateProccessInstanceReply> CreateStorageInstance(
                                    CreateStorageInstanceRequest request, ServerCallContext context)
            {
                Console.WriteLine("Creating Storage: " + request.MyData.ServerName);
                return Task.FromResult(CreateStorInstance(request));

            }

            private CreateProccessInstanceReply CreateStorInstance(CreateStorageInstanceRequest request)
            {
                int port;
                string hostname;
                string urlRefined;
                urlRefined = request.MyData.Url.Split("http://")[1];
                port = Convert.ToInt32(urlRefined.Split(':')[1]);
                hostname = urlRefined.Split(':')[0];
                string workingDirectory = Path.GetFullPath("storage.exe");
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = workingDirectory,
                    UseShellExecute = true,
                    CreateNoWindow = false,
                };
                psi.Arguments = Convert.ToString(request.ServerId) + " " + request.MyData.ServerName + " " + hostname + " " + port + " " + request.GossipDelay;
                foreach (ProccessData data in request.DependenciesData)
                {
                    psi.Arguments = psi.Arguments + " " + data.ServerName + "|" + data.Url;
                }
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
