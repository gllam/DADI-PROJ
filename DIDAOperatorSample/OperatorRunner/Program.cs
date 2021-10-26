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
        List<string> workerPorts = new List<string>();

        public WorkerService() { }

        public override Task<SendDIDAReqReply> SendDIDAReq(SendDIDAReqRequest request, ServerCallContext context)
        {
            return Task.FromResult(SendDIDA(request));
        }

        public SendDIDAReqReply SendDIDA(SendDIDAReqRequest request)
        {
            Console.WriteLine(request);
            string classname = request.Asschain[request.Next].Opid.Classname;
            DIDAMetaRecord metarecord = new DIDAMetaRecord
            {
                id = request.Meta.Id
            }; //dummy meta record
            string input = request.Input;
            string previousoutput = request.Asschain[request.Next - 1].Output;

            request.Asschain[request.Next].Output = RunOperator(classname, metarecord, input, previousoutput); //metarecord?
            request.Next += 1;
            SendRequestToWorker(request);

            return new SendDIDAReqReply
            {
                Ack = true
            };
        }

        string RunOperator(string classname, DIDAMetaRecord meta, string input, string previousoutput)
        {
            string _currWorkingDir = Directory.GetCurrentDirectory();
            IDIDAOperator _opLoadedByReflection;
            string filename = classname + ".dll";
            Assembly _dll = Assembly.LoadFrom(filename);
            Type type = _dll.GetType(classname);
            _opLoadedByReflection = (IDIDAOperator)Activator.CreateInstance(type);
            _opLoadedByReflection.ConfigureStorage(new DIDAStorageNode[] { new DIDAStorageNode { host = "localhost", port = 2001, serverId = "s1" } }, MyLocationFunction);
            string output = _opLoadedByReflection.ProcessRecord(meta,input,previousoutput);
            return output;
        }

        void SendRequestToWorker(SendDIDAReqRequest request)
        {
            string host = request.Asschain[request.Next].Host;
            int port = request.Asschain[request.Next].Port;
            GrpcChannel channel = GrpcChannel.ForAddress(host + ":" + port);
            DIDAWorkerService.DIDAWorkerServiceClient client = new DIDAWorkerService.DIDAWorkerServiceClient(channel);
            SendDIDAReqReply reply = client.SendDIDAReq(request);
            Console.WriteLine("Request to worker " + reply.Ack);
        }

        private static DIDAStorageNode MyLocationFunction(string id, OperationType type)
        {
            return new DIDAStorageNode { host = "localhost", port = 2001, serverId = "s1" };
        }

        class Program
        {
            static void Main(string[] args)
            {
                const int port = 5001;
                const string host = "localhost";

                ServerPort serverPort = new ServerPort(host, port, ServerCredentials.Insecure); ;

                Server server = new Server
                {
                    Services = { DIDAWorkerService.BindService(new WorkerService()) },
                    Ports = { serverPort }
                };

                server.Start();
                Console.ReadLine();
            }         
        }
    }
}