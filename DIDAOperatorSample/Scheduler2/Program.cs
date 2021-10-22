using DIDAWorker;
using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Scheduler
{

    public class SchedulerService : DIDASchedulerService.DIDASchedulerServiceBase
    {
        //Dictionary<string, GrpcChannel> _workerChannels = new Dictionary<string, GrpcChannel>();
        List<string> workerPorts = new List<string>();

        public SchedulerService(){}

        public override Task<SendScriptReply> SendScript(SendScriptRequest request, ServerCallContext context)
        {
            return Task.FromResult(requestApp(request));
        }

        public SendScriptReply requestApp(SendScriptRequest request)
        {
            Console.WriteLine(request.Input);
            MetaRecord meta = new MetaRecord
            {
                Id = 1
            }; //metarecord dummy
            int chainSize = request.App.Count;
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
                    Port = Int32.Parse(workerPorts[opIndex]),
                    Output = null
                };
                req.Asschain[opIndex] = ass;
            }

            return new SendScriptReply
            {
                Ack = true
            }; //reply to pm
        }

        public void SendRequestToWorker(SendDIDAReqRequest request)
        {
            string serverHostname = "localhost";
            GrpcChannel channel = GrpcChannel.ForAddress("http://" + serverHostname + ":" + workerPorts[0]);
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
            foreach (string port in request.Ports)
            {
                workerPorts.Add(port);
            }
            return new SendWorkersReply
            {
                Ack = true
            };
        }
        
    }

    class Program
    {
        static void Main(string[] args)
        {
            const int port = 4001;
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
            Console.ReadKey();
        }
    }
}
