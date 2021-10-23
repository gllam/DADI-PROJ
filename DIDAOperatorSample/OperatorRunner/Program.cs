using DIDAOperator;
using DIDAWorker;
using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
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

        public SendDIDAReqReply SendDIDA(SendDIDAReqRequest req)
        {

            MetaRecord meta = new MetaRecord
            {
                Id = 1
            };

            SendDIDAReqRequest request = new SendDIDAReqRequest
            {
                Meta = meta,
                Input = req.Input,
                Next = 0,
                ChainSize = req.ChainSize
            };

            foreach (Assignment assignment in req.Asschain)
            {
                OperatorID oper = new OperatorID
                {
                    Classname = assignment.Opid.Classname,
                    Order = assignment.Opid.Order
                };
                Assignment assignment1 = new Assignment
                {
                    Opid = oper,
                    Host = assignment.Host, //should be automatically localhost?
                    Port = assignment.Port,
                    Output = assignment.Output //should logo be null?
                };
                workerPorts.Add(assignment.Port.ToString());

                req.Asschain[oper.Order] = assignment1;
            }

            return new SendDIDAReqReply
            {
                Ack = true
            };

            void SendRequestToWorker(SendDIDAReqRequest req)
            {
                string serverHostname = "localhost";
                GrpcChannel channel = GrpcChannel.ForAddress("http://" + serverHostname + ":" + workerPorts[0]);
                DIDAWorkerService.DIDAWorkerServiceClient client = new DIDAWorkerService.DIDAWorkerServiceClient(channel);
                SendDIDAReqReply reply = client.SendDIDAReq(req);
                Console.WriteLine("Request to worker " + reply.Ack);
            }

        }
        class Program
        {
            static void Main(string[] args)
            {
                const int port = 4001;
                const string host = "localhost";
                string startupMessage;
                ServerPort serverPort = new ServerPort(host, port, ServerCredentials.Insecure); ;

                Server server = new Server
                {
                    Services = { DIDAWorkerService.BindService(new WorkerService()) },
                    Ports = { serverPort }
                };

                server.Start();

                IDIDAOperator op = new CounterOperator();
                DIDAMetaRecord meta = new DIDAMetaRecord { id = 1 };
                op.ConfigureStorage(new DIDAStorageNode[] { new DIDAStorageNode { host = "localhost", port = 2001, serverId = "s1" } }, MyLocationFunction);
                string result = op.ProcessRecord(meta, "sample_input", "sample_previous_output");
                Console.WriteLine("result: " + result);
                Console.ReadLine();

            }

            private static DIDAStorageNode MyLocationFunction(string id, OperationType type)
            {
                return new DIDAStorageNode { host = "localhost", port = 2001, serverId = "s1" };
            }
        }
    }
}