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
        public string name;
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

        public override Task<SendAppDataReply> SendAppData(SendAppDataRequest request, ServerCallContext context)
        {
            return Task.FromResult(RequestApp(request));
        }

        public override Task<StatusReply> Status(Empty request, ServerCallContext context)
        {
            return Task.FromResult(StatusOperation());
        }

        private StatusReply StatusOperation()
        {
            Console.WriteLine("Scheduler: " + this.name + " -> I am alive!");
            StatusReply reply = new StatusReply { Success = true };
            return reply;
        }

        public SendAppDataReply RequestApp(SendAppDataRequest request)
        {
            Console.WriteLine(this);
            try
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
                for (int opIndex = lastWorkerIndex + 1; opIndex != chainSize + lastWorkerIndex + 1; opIndex += 1)
                { 
                    var app = request.App[opIndex % workerMap.Count];
                    Worker worker = workerMap[opIndex % workerMap.Count];
                    Console.WriteLine(app);
                    OperatorID op = new OperatorID
                    {
                        Classname = app.Split()[1],
                        Order = Int32.Parse(app.Split()[2])
                    };
                    Assignment ass = new Assignment
                    {
                        Opid = op,
                        Host = worker.host,
                        Port = Int32.Parse(worker.port),
                        Output = ""
                    };
                    req.Asschain.Add(ass);
                    lastWorkerIndex = opIndex % workerMap.Count;
                }
                SendRequestToWorker(req);
                return new SendAppDataReply
                {
                    Ack = true
                }; //reply to pm
            } catch (Exception e)
            {
                Console.WriteLine(e);
                return new SendAppDataReply
                {
                    Ack = false
                };
            }
        }

        public void SendRequestToWorker(SendDIDAReqRequest request)
        {
            try
            {
                string host = request.Asschain[request.Next].Host;
                int port = request.Asschain[request.Next].Port;
                Worker target = workerMap.Find(x => x.host == host && x.port == Convert.ToString(port));

                SendDIDAReqReply reply = target.GetClient().SendDIDAReq(request);
                Console.WriteLine("Request to worker " + reply.Ack);
            } catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        internal void AddWorker(string input)
        {
            string[] data = input.Split("|");
            string[] hostport = data[1].Split("//")[1].Split(":");
            workerMap.Add(new Worker(data[0], hostport[0], hostport[1]));
            Console.WriteLine(hostport[0], hostport[1]);
        }

        public override string ToString()
        {
            return "Scheduler " + name + " Workers: " + ListWorkers();
        }

        public string ListWorkers()
        {
            string s_data = "";
            foreach (var worker in workerMap)
            {
                s_data += worker.name + "-" + worker.host + ":" + worker.port + " ";
            }
            return s_data;
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
