using DIDAWorker;
using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace SchedulerNamespace
{

    public class SchedulerService : DIDASchedulerService.DIDASchedulerServiceBase
    {
        //Dictionary<string, GrpcChannel> _workerChannels = new Dictionary<string, GrpcChannel>();
        List<(string, string)> workerMap = new List<(string, string)>();

        public SchedulerService() { }

        public override Task<SendAppDataReply> SendAppData(SendAppDataRequest request, ServerCallContext context)
        {
            return Task.FromResult(RequestApp(request));
        }

        public SendAppDataReply RequestApp(SendAppDataRequest request)
        {
            Console.WriteLine(request.Input);
            MetaRecord meta = new MetaRecord
            {
                Id = 1
            }; //metarecord dummy
            int chainSize = request.App.Count;
            Console.WriteLine(chainSize);
            SendDIDAReqRequest req = new SendDIDAReqRequest
            {
                Meta = meta,
                Input = request.Input,
                Next = 0,
                ChainSize = chainSize
            }; //request to worker
            for (int opIndex = 0; opIndex < chainSize; opIndex++)
            {
                OperatorID op = new OperatorID
                {
                    Classname = request.App[opIndex].Split()[1],
                    Order = Int32.Parse(request.App[opIndex].Split()[2])
                };
                Assignment ass = new Assignment
                {
                    Opid = op,
                    Host = "localhost",
                    Port = 5001,
                    //Host = workerMap[opIndex].Item1,
                    //Port = Int32.Parse(workerMap[opIndex].Item2),
                    Output = ""
                };
                req.Asschain.Add(ass);
            }
            SendRequestToWorker(req);
            return new SendAppDataReply
            {
                Ack = true
            }; //reply to pm
        }

        public void SendRequestToWorker(SendDIDAReqRequest request)
        {
            string host = request.Asschain[request.Next].Host;
            int port = request.Asschain[request.Next].Port;
            GrpcChannel channel = GrpcChannel.ForAddress("http://" + host + ":" + port);
            DIDAWorkerService.DIDAWorkerServiceClient client = new DIDAWorkerService.DIDAWorkerServiceClient(channel);
            SendDIDAReqReply reply = client.SendDIDAReq(request);
            Console.WriteLine("Request to worker " + reply.Ack);
        }


        public override Task<SendWorkersReply> SendWorkers(SendWorkersRequest request, ServerCallContext context)
        {
            return Task.FromResult(setWorkers(request));
        }


        public SendWorkersReply setWorkers(SendWorkersRequest request)
        {
            foreach (string url in request.Url)
            {
                string[] hostport = url.Split("//")[1].Split(":");
                workerMap.Add((hostport[0], hostport[1]));
            }
            return new SendWorkersReply
            {
                Ack = true
            };
        }

    }

    public class Scheduler
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("HELLO");
            string serverId = args[0];
            string hostname = args[1];
            int port = Convert.ToInt32(args[2]);
            ServerPort serverPort;
            string startupMessage;
            serverPort = new ServerPort(hostname, port, ServerCredentials.Insecure);
            startupMessage = "Insecure Scheduler server '" + serverId + "' | hostname: " + hostname + " | port " + port;

            Server server = new Server
            {
                Services = { DIDASchedulerService.BindService(new SchedulerService()) },
                Ports = { serverPort }
            };

            server.Start();

            Console.WriteLine(startupMessage);
            //Configuring HTTP for client connections in Register method
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            Console.ReadKey();
            /*const int port = 4001;
            const string hostname = "localhost";
            string startupMessage;
            ServerPort serverPort;

            serverPort = new ServerPort(hostname, port, ServerCredentials.Insecure);
            startupMessage = "Insecure Scheduler server listening on port " + port;

            Server server = new Server
            {
                Services = { DIDASchedulerService.BindService(new SchedulerService()) },
                Ports = { serverPort }
            };

            server.Start();

            Console.WriteLine(startupMessage);
            //Configuring HTTP for client connections in Register method
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            Console.ReadKey();*/
        }

        public void Initialize(string serverId, string hostname, int port)
        {
            /*ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = @"C:\Users\david\Desktop\Aulas\Mestrado\1º ano\PADI\Proj\DADI-PROJ\DIDAOperatorSample\Scheduler2\bin\Debug\netcoreapp3.1\Scheduler.exe",
                UseShellExecute = true,
                CreateNoWindow = false
            };
            Process.Start(psi);*/
            /*
            Process myProcess = new Process();
            myProcess.StartInfo.CreateNoWindow = false;
            myProcess.StartInfo.FileName = @"C:\Users\david\Desktop\Aulas\Mestrado\1º ano\PADI\Proj\DADI-PROJ\DIDAOperatorSample\Scheduler2\bin\Debug\netcoreapp3.1\Scheduler.exe";
            myProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            myProcess.Start();*/

            //this.Main(args);
            /*this.serverId = serverId;
            this.hostname = hostname;
            this.port = port;
            ServerPort serverPort;
            string startupMessage;
            serverPort = new ServerPort(hostname, port, ServerCredentials.Insecure);
            startupMessage = "Insecure Scheduler server listening on port " + port;

            Server server = new Server
            {
                Services = { DIDASchedulerService.BindService(new SchedulerService()) },
                Ports = { serverPort }
            };

            server.Start();

            Console.WriteLine(startupMessage);
            //Configuring HTTP for client connections in Register method
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            Console.ReadKey();*/
        }
    }
}
