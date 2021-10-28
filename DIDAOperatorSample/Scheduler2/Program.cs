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
                    //Host = "localhost",
                    //Port = 5001,
                    Host = workerMap[opIndex].Item1,
                    Port = Int32.Parse(workerMap[opIndex].Item2),
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

        internal void AddWorker(string workerUrl)
        {
            string[] hostport = workerUrl.Split("//")[1].Split(":");
            workerMap.Add((hostport[0], hostport[1]));
            Console.WriteLine(hostport[0], hostport[1]);
        }
    }

    public class Scheduler
    {
        public static void Main(string[] args)
        {
            Console.WriteLine(args.Length);
            SchedulerService scheduler = new SchedulerService();
            int serverId = Convert.ToInt32(args[0]);
            string hostname = args[1];
            int port = Convert.ToInt32(args[2]);
            for(int i = 3; i < args.Length; i++)
            {
                scheduler.AddWorker(args[i]);
            }
            ServerPort serverPort;
            string startupMessage;
            serverPort = new ServerPort(hostname, port, ServerCredentials.Insecure);
            startupMessage = "Insecure Scheduler server '" + serverId + "' | hostname: " + hostname + " | port " + port;
            
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
