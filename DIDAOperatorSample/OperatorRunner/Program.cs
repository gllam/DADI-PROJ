using DIDAOperator;
using DIDAWorker;
using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace OperatorRunner
{

    public class WorkerService : DIDAWorkerService.DIDAWorkerServiceBase
    {
        List<(string, string)> storageMap = new List<(string, string)>();
        int gossipDelay;

        public WorkerService(int gossipDelay, string id) {
            this.gossipDelay = gossipDelay;
        }

        public override Task<SendDIDAReqReply> SendDIDAReq(SendDIDAReqRequest request, ServerCallContext context)
        {
            return Task.FromResult(SendDIDA(request));
        }

        public override Task<WorkerStatusReply> Status(WorkerStatusEmpty request, ServerCallContext context)
        {
            return Task.FromResult(StatusOperation());
        }

        private WorkerStatusReply StatusOperation()
        {
            Console.WriteLine("I am a nice and well alive Worker!");
            WorkerStatusReply reply = new WorkerStatusReply { Success = true };
            return reply;
        }

        public SendDIDAReqReply SendDIDA(SendDIDAReqRequest request) //out of range
        {
            Console.WriteLine(request);
            string classname = request.Asschain[request.Next].Opid.Classname;
            Console.WriteLine(classname);
            DIDAMetaRecord metarecord = new DIDAMetaRecord
            {
                id = request.Meta.Id
            }; //dummy meta record
            string input = request.Input;
            string previousoutput = null;
            if (request.Next != 0)
            {
                previousoutput = request.Asschain[request.Next - 1].Output;
            }
            request.Asschain[request.Next].Output = RunOperator(classname, metarecord, input, previousoutput); //metarecord?
            request.Next += 1;
            if (request.Next < request.Asschain.Count)
            {
                SendRequestToWorker(request);
            }
            return new SendDIDAReqReply
            {
                Ack = true
            };
        }

        string RunOperator(string classname, DIDAMetaRecord meta, string input, string previousoutput)
        {
            Console.WriteLine(classname);
            string _currWorkingDir = Directory.GetCurrentDirectory();
            IDIDAOperator _opLoadedByReflection;
            string filename = classname + ".dll";
            Assembly _dll = Assembly.LoadFrom(filename);
            Type[] types = _dll.GetTypes();
            Type t = null;
            foreach (Type type in types)
            {
                if (type.Name == "CounterOperator")
                {
                    t = type;
                }
            }
            Console.WriteLine(t);
            _opLoadedByReflection = (IDIDAOperator)Activator.CreateInstance(t);
            _opLoadedByReflection.ConfigureStorage(new DIDAStorageNode[] { new DIDAStorageNode { host = "localhost", port = 2001, serverId = "s1" } }, MyLocationFunction);
            string output = _opLoadedByReflection.ProcessRecord(meta, input, previousoutput);
            return output;
        }

        void SendRequestToWorker(SendDIDAReqRequest request)
        {
            string host = request.Asschain[request.Next].Host;
            int port = request.Asschain[request.Next].Port;
            GrpcChannel channel = GrpcChannel.ForAddress("http://" + host + ":" + port);
            DIDAWorkerService.DIDAWorkerServiceClient client = new DIDAWorkerService.DIDAWorkerServiceClient(channel);
            SendDIDAReqReply reply = client.SendDIDAReq(request);
            Console.WriteLine("Request to worker " + reply.Ack);
        }

        private static DIDAStorageNode MyLocationFunction(string id, OperationType type)
        {
            return new DIDAStorageNode { host = "localhost", port = 2001, serverId = "s1" };
        }

        internal void AddStorage(string storageUrl)
        {
            string[] hostport = storageUrl.Split("//")[1].Split(":");
            storageMap.Add((hostport[0], hostport[1]));
            Console.WriteLine(hostport[0], hostport[1]);
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            string replicaId = args[0];
            int port = Convert.ToInt32(args[2]);
            string host = args[1];
            int gossipDelay = Convert.ToInt32(args[3]);
            WorkerService workerService = new WorkerService(gossipDelay,replicaId);
            for (int i = 4; i < args.Length; i++)
            {
                workerService.AddStorage(args[i]);
            }
            ServerPort serverPort = new ServerPort(host, port, ServerCredentials.Insecure);

            Console.WriteLine("Insecure Worker server '" + replicaId + "' | hostname: " + host + " | port " + port);

            Server server = new Server
            {
                Services = { DIDAWorkerService.BindService(workerService) },
                Ports = { serverPort }
            };

            server.Start();
            Console.ReadLine();
        }
    }
}