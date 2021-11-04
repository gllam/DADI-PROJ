﻿using DIDAOperator;
using DIDAWorker;
using Grpc.Core;
using Grpc.Net.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Worker;

namespace OperatorRunner
{

    public class WorkerService : DIDAWorkerService.DIDAWorkerServiceBase
    {
        List<DIDAStorageNode> storageMap = new List<DIDAStorageNode>();
        int gossipDelay;
        string name;

        public WorkerService(int gossipDelay, string name) {
            this.gossipDelay = gossipDelay;
            this.name = name;
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
            Console.WriteLine("Worker: " + this.name + " -> I am alive!");
            WorkerStatusReply reply = new WorkerStatusReply { Success = true };
            return reply;
        }

        public SendDIDAReqReply SendDIDA(SendDIDAReqRequest request) //out of range
        {
            try
            {
                Console.WriteLine(this);
                Console.WriteLine(request);
                string classname = request.Asschain[request.Next].Opid.Classname;
                Console.WriteLine(classname);
                DIDAMetaRecord metarecord = new DIDAMetaRecord
                {
                    Id = request.Meta.Id
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
            } catch (Exception e)
            {
                Console.WriteLine(e);
                return new SendDIDAReqReply
                {
                    Ack = false
                };
            }
        }

        string RunOperator(string classname, DIDAMetaRecord meta, string input, string previousoutput)
        {
            Console.WriteLine(this);
            Console.WriteLine(classname);
            string _currWorkingDir = Directory.GetCurrentDirectory();
            IDIDAOperator _opLoadedByReflection;
            //string path = Path.Combine(", );
            //Console.WriteLine(path);
            Assembly _dll = Assembly.LoadFrom("..//..//..//..//DebugFiles//LibOperators.dll"); //maybe name not static
            Type[] types = _dll.GetTypes();
            Type t = null;
            foreach (Type type in types)
            {
                if (type.Name.Contains(classname))
                {
                    t = type;
                }
            }
            Console.WriteLine(t);
            _opLoadedByReflection = (IDIDAOperator)Activator.CreateInstance(t);
            _opLoadedByReflection.ConfigureStorage(new StorageProxy(storageMap.ToArray(), meta));
            string output = _opLoadedByReflection.ProcessRecord(meta, input, previousoutput);
            return output;
        }

        void SendRequestToWorker(SendDIDAReqRequest request)
        {
            System.Threading.Thread.Sleep(gossipDelay);
            string host = request.Asschain[request.Next].Host;
            int port = request.Asschain[request.Next].Port;
            GrpcChannel channel = GrpcChannel.ForAddress("http://" + host + ":" + port);
            DIDAWorkerService.DIDAWorkerServiceClient client = new DIDAWorkerService.DIDAWorkerServiceClient(channel);
            SendDIDAReqReply reply = client.SendDIDAReq(request);
            Console.WriteLine("Request to worker " + reply.Ack);
        }

        internal void AddStorage(string storage)
        {
            string[] storageUrl = storage.Split("|");
            string[] hostport = storageUrl[1].Split("//")[1].Split(":");
            DIDAStorageNode node = new DIDAStorageNode
            {
                serverId = storageUrl[0],
                host = hostport[0],
                port = Convert.ToInt32(hostport[1])
            };
            storageMap.Add(node);
        }

        public override string ToString()
        {
            return "Worker " + name + " GossipDelay " + gossipDelay + " Storages: " + ListStorages();
        }

        public string ListStorages()
        {
            string s_data = "";
            foreach (var storage in storageMap)
            {
                s_data += storage.serverId + "-" + storage.host + ":" + storage.port + " ";
            }
            return s_data;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            int replicaId = Convert.ToInt32(args[0]);
            string workerName = args[1];
            string host = args[2];
            int port = Convert.ToInt32(args[3]);
            int gossipDelay = Convert.ToInt32(args[4]);
            WorkerService workerService = new WorkerService(gossipDelay,workerName);
            for (int i = 5; i < args.Length; i++)
            {
                workerService.AddStorage(args[i]);
            }
            ServerPort serverPort = new ServerPort(host, port, ServerCredentials.Insecure);

            Console.WriteLine("Insecure Worker server '" + workerName + "' | hostname: " + host + " | port " + port);

            Server server = new Server
            {
                Services = { DIDAWorkerService.BindService(workerService) },
                Ports = { serverPort }
            };

            server.Start();
            while (true) ;
        }
    }
}