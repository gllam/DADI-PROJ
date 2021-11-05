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
    public class Worker
    {
        public string host;
        public string port;
        private string name;
        private DIDAWorkerService.DIDAWorkerServiceClient client;

        public Worker(string name,string host, string port)
        {
            this.host = host;
            this.port = port;
            this.name = name;
        }
        public void SetClient(DIDAWorkerService.DIDAWorkerServiceClient client)
        {
            this.client = client;
        }
        public DIDAWorkerService.DIDAWorkerServiceClient GetClient()
        {
            if(this.client == null)
            {
                GrpcChannel channel = GrpcChannel.ForAddress("http://" + this.host + ":" + this.port);
                this.client = new DIDAWorkerService.DIDAWorkerServiceClient(channel);
            }
            return this.client;
        }
    }
    public class SchedulerService : DIDASchedulerService.DIDASchedulerServiceBase
    {
        //Dictionary<string, GrpcChannel> _workerChannels = new Dictionary<string, GrpcChannel>();
        List<Worker> workerMap = new List<Worker>();
        int lastWorkerIndex = 0;
        string name;


        public SchedulerService(string name) { this.name = name; }

        override
        public string ToString()
        {
            return "TODO " + this.name;
        }
        public override Task<ListServerSchedReply> ListServer(Empty request, ServerCallContext context)
        {
            return Task.FromResult(LiServer());
        }

        public override Task<SendAppDataReply> SendAppData(SendAppDataRequest request, ServerCallContext context)
        {
            return Task.FromResult(RequestApp(request));
        }

        public override Task<StatusReply> Status(Empty request, ServerCallContext context)
        {
            return Task.FromResult(StatusOperation());
        }

        private ListServerSchedReply LiServer()
        {
            return new ListServerSchedReply { ServerDataToString = this.ToString() };
        }

        private StatusReply StatusOperation()
        {
            Console.WriteLine("Scheduler: " + this.name + " -> I am alive!");
            StatusReply reply = new StatusReply { Success = true };
            return reply;
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
            for (int opIndex = lastWorkerIndex + 1 % workerMap.Count; opIndex < chainSize; opIndex = opIndex + 1 % workerMap.Count)
            {
                OperatorID op = new OperatorID
                {
                    Classname = request.App[opIndex].Split()[1],
                    Order = Int32.Parse(request.App[opIndex].Split()[2])
                };
                Assignment ass = new Assignment
                {
                    Opid = op,
                    Host = workerMap[opIndex].host,
                    Port = Int32.Parse(workerMap[opIndex].port),
                    Output = ""
                };
                req.Asschain.Add(ass);
                lastWorkerIndex = opIndex;
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
            Worker target = workerMap.Find(x => x.host == host && x.port == Convert.ToString(port));

            SendDIDAReqReply reply = target.GetClient().SendDIDAReq(request);
            Console.WriteLine("Request to worker " + reply.Ack);
        }


        /*public override Task<SendWorkersReply> SendWorkers(SendWorkersRequest request, ServerCallContext context)
        {
            return Task.FromResult(setWorkers(request));
        }*/


        /*public SendWorkersReply setWorkers(SendWorkersRequest request)
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
        }*/

        internal void AddWorker(string input)
        {
            string[] data = input.Split("|");
            string[] hostport = data[1].Split("//")[1].Split(":");
            workerMap.Add(new Worker(data[0], hostport[0], hostport[1]));
            Console.WriteLine(hostport[0], hostport[1]);
        }
    }

    public class Scheduler
    {
        public static void Main(string[] args)
        {
            Console.WriteLine(args.Length);
            int serverId = Convert.ToInt32(args[0]);
            string schedulerName = args[1];
            string hostname = args[2];
            int port = Convert.ToInt32(args[3]);
            SchedulerService scheduler = new SchedulerService(schedulerName);
            for (int i = 4; i < args.Length; i++)
            {
                scheduler.AddWorker(args[i]);
            }
            ServerPort serverPort;
            string startupMessage;
            serverPort = new ServerPort(hostname, port, ServerCredentials.Insecure);
            startupMessage = "Insecure Scheduler server '" + schedulerName + "' | hostname: " + hostname + " | port " + port;

            Server server = new Server
            {
                Services = { DIDASchedulerService.BindService(scheduler) },
                Ports = { serverPort }
            };

            server.Start();

            Console.WriteLine(startupMessage);
            //Configuring HTTP for client connections in Register method
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            Console.ReadKey();
        }
    }
}
