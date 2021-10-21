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

        public SchedulerService()
        {
            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
        }

        public override Task<SendScriptReply> SendScript(SendScriptRequest request, ServerCallContext context)
        {
            return Task.FromResult(requestApp(request));
        }

        public SendScriptReply requestApp(SendScriptRequest request)
        {
            MetaRecord meta = new MetaRecord
            {
                Id = 1
            };
            int chainSize = request.App.Count;
            SendDIDAReqRequest req = new SendDIDAReqRequest
            {
                Meta = meta,
                Input = request.Input,
                Next = 0,
                ChainSize = chainSize
            };
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
            };
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
            Console.ReadKey();
        }
    }
}
