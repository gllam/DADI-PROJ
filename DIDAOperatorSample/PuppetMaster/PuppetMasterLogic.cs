using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;

namespace PuppetMaster
{
    public class ProcessCreatorAsServer
    {
        private readonly GrpcChannel channel;
        private readonly DIDAProccessCreatorService.DIDAProccessCreatorServiceClient client;
        private readonly Form1 guiWindow;

        public ProcessCreatorAsServer(Form1 guiWindow)
        {
            this.guiWindow = guiWindow;
            // setup the client side

            AppContext.SetSwitch(
                "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            channel = GrpcChannel.ForAddress("http://localhost:10000");

            client = new DIDAProccessCreatorService.DIDAProccessCreatorServiceClient(channel);
        }

        internal int SendCreateSchedulerRequest(int replicaId, string[] scheduler, string[][] workers, List<String> workersMap)
        {
            CreateSchedulerInstanceRequest request = new CreateSchedulerInstanceRequest
            {
                ServerId = replicaId,
                MyData = new ProccessData
                {
                    ServerName = scheduler[1],
                    Url = scheduler[2]
                }
            };
;
            foreach (string[] worker in workers)
            {
                request.DependenciesData.Add(new ProccessData
                { 
                    ServerName = worker[1],
                    Url = worker[2]
                });
            }

            CreateProccessInstanceReply reply = client.CreateSchedulerInstance(request);
            return reply.Id;
        }

        internal int SendCreateWorkerRequest(int replicaId,string[] worker, string[][] storages, List<string> storageMap)
        {
            CreateWorkerInstanceRequest request = new CreateWorkerInstanceRequest
            {
                ServerId = replicaId,
                MyData = new ProccessData
                {
                    ServerName = worker[1],
                    Url = worker[2],
                },
                GossipDelay = worker[3] 
            };

            foreach (string[] storage in storages)
            {
                request.DependenciesData.Add(new ProccessData
                {
                    ServerName = storage[1],
                    Url = storage[2]
                });
            }

            CreateProccessInstanceReply reply = client.CreateWorkerInstance(request);
            return reply.Id;
        }

        internal int SendCreateStorageRequest(int replicaId,string[] storage, string[][] storages)
        {
            CreateStorageInstanceRequest request = new CreateStorageInstanceRequest
            {
                ServerId = replicaId,
                MyData = new ProccessData
                {
                    ServerName = storage[1],
                    Url = storage[2],
                },
                GossipDelay = storage[3]
            };

            foreach (string[] storageData in storages)
            {
                request.DependenciesData.Add(new ProccessData
                {
                    ServerName = storageData[1],
                    Url = storageData[2]
                });
            }

            CreateProccessInstanceReply reply = client.CreateStorageInstance(request);
            return reply.Id;
        }
    }

    public class SchedulerAsServer
    {
        private readonly GrpcChannel channel;
        private readonly DIDASchedulerService.DIDASchedulerServiceClient client;
        public readonly string serverId;

        public SchedulerAsServer(string serverId, string hostname, string port)
        {
            this.serverId = serverId;
            // setup the client side

            AppContext.SetSwitch(
                "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            channel = GrpcChannel.ForAddress("http://" + hostname + ":" + port);

            client = new DIDASchedulerService.DIDASchedulerServiceClient(channel);
        }

        override
        public string ToString(){
            return "\r\n" + this.serverId + " -> client: " + this.client.ToString() + "\r\n";
        }
        public Boolean SendAppData(string[] data)
        {
            SendAppDataRequest request = new SendAppDataRequest
            {
                Input = data[1],
            };
            //need to check where we put the input files
            foreach (string line in System.IO.File.ReadLines(Path.GetFullPath(data[2])))
            {
                request.App.Add(line);
            }

            SendAppDataReply reply = client.SendAppData(request);
            return reply.Ack;
        }

        public bool SendStatusRequest()
        {
            Empty request = new Empty { };
            try
            {
                StatusReply reply = client.Status(request);

                return reply.Success;
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal string SendListServerRequest()
        {
            Empty request = new Empty { };
            try
            {
                ListServerSchedReply reply = client.ListServer(request);

                return reply.ServerDataToString;
            }
            catch (Exception)
            {
                return this.serverId + " is not alive!";
            }
        }
    }
    //Only used to populate and status
    public class StorageAsServer
    {
        private readonly GrpcChannel channel;
        private readonly DIDAStorageService.DIDAStorageServiceClient client;
        public readonly string serverId;

        public StorageAsServer(string serverId, string hostname, string port)
        {
            this.serverId = serverId;
            // setup the client side

            AppContext.SetSwitch(
                "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            channel = GrpcChannel.ForAddress("http://" + hostname + ":" + port);

            client = new DIDAStorageService.DIDAStorageServiceClient(channel);
        }

        override
        public string ToString()
        {
            return "\r\n" + this.serverId + " -> client: " + this.client.ToString() + "\r\n";
        }

        public Boolean SendWriteRequest(string key, string value)
        {
            DIDAWriteRequest request = new DIDAWriteRequest
            {
                Id = key,
                Val = value
            };
            DIDAVersion data = client.write(request);
            if (data == null)
                return false;
            return true;
        }

        public bool SendStatusRequest()
        {
            StorageStatusEmpty request = new StorageStatusEmpty { };
            try
            {
                StorageStatusReply reply = client.Status(request);

                return reply.Success;
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal string SendListServerRequest()
        {
            StorageStatusEmpty request = new StorageStatusEmpty { };
            try
            {
                ListServerStorageReply reply = client.ListServer(request);

                return reply.SereverDataToSTring;
            }
            catch (Exception)
            {
                return this.serverId + " is not alive!";
            }
        }
    }

    public class WorkerAsServer
    {
        private readonly GrpcChannel channel;
        private readonly DIDAWorkerService.DIDAWorkerServiceClient client;
        public readonly string serverId;

        public WorkerAsServer(string serverId, string hostname, string port)
        {
            this.serverId = serverId;
            // setup the client side

            AppContext.SetSwitch(
                "System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            channel = GrpcChannel.ForAddress("http://" + hostname + ":" + port);

            client = new DIDAWorkerService.DIDAWorkerServiceClient(channel);
        }

        override
        public string ToString()
        {
            return "\r\n" + this.serverId + " -> client: " + this.client.ToString() + "\r\n";
        }

        public bool SendStatusRequest()
        {
            WorkerStatusEmpty request = new WorkerStatusEmpty { };
            try
            {
                WorkerStatusReply reply = client.Status(request);

                return reply.Success;
            }
            catch (Exception)
            {
                return false;
            }
        }

        internal string SendListServerRequest()
        {
            WorkerStatusEmpty request = new WorkerStatusEmpty { };
            try
            {
                ListServerWorkerReply reply = client.ListServer(request);

                return reply.SereverDataToSTring;
            }
            catch (Exception)
            {
                return this.serverId + " is not alive!";
            }
        }
    }

    public class ProcessInfo
    {
        public int processId;
        public string serverName;

        public ProcessInfo(string serverName, int processId)
        {
            this.processId = processId;
            this.serverName = serverName;
        }

        override
        public string ToString()
        {
            return this.serverName + " -> " + this.processId;
        }
    }

    public delegate void DelAddMsg(string line);

    class PuppetMasterLogic : DIDAWorkerService.DIDAWorkerServiceBase
    {
        private bool debugMode = false;
        private SchedulerAsServer scheduler;
        private ProcessCreatorAsServer pcs;
        private List<StorageAsServer> storagesAsServers = new List<StorageAsServer>();
        private List<WorkerAsServer> workersAsServers = new List<WorkerAsServer>();
        private List<string> schedulerMap = new List<string>();
        private List<string> workerMap = new List<string>();
        private List<string> storageMap = new List<string>();
        private List<ProcessInfo> processesInfo = new List<ProcessInfo>();
        Form1 guiWindow;

        public PuppetMasterLogic(Form1 guiWindow) {
            this.guiWindow = guiWindow;
            pcs = new ProcessCreatorAsServer(guiWindow);
        }
        //PuppetMasterAsServer




        //End PuppetMasterAsServer

        internal void SetDebugMode(bool debugMode)
        {
            this.debugMode = debugMode;
            if(debugMode == true)
            {
                ServerPort serverPort = new ServerPort("localhost", 10001, ServerCredentials.Insecure);

                Server server = new Server
                {
                    Services = { DIDAWorkerService.BindService(this) },
                    Ports = { serverPort }
                };

                server.Start();
            }
        }

        override
        public string ToString()
        {
            return "Puppet Master:\r\n" +
                   "\r\nDebugMode: " + debugMode +
                   "\r\nSchedulerAsServer: " + string.Join("\r\n", scheduler) +
                   "\r\nWorkersAsServer: " + string.Join("\r\n", workersAsServers) +
                   "\r\nStoragesAsServer: " + string.Join("\r\n", storagesAsServers) +
                   "\r\nSchedulerMap: " + string.Join(",", schedulerMap) +
                   "\r\nWorkerMap: " + string.Join(",", workerMap) +
                   "\r\nStorageMap: " + string.Join(",", storageMap) +
                   "\r\nProcessesInfo:\r\n" + string.Join("\r\n", processesInfo) +
                   "\r\nPuppet Master END\r\n";
        }
        public void CreateChannelWithServer(string serverId, string url, string type)
        {
            string urlRefined = url.Split("http://")[1];
            string port = urlRefined.Split(':')[1];
            string hostname = urlRefined.Split(':')[0];
            if (type == "scheduler")
                scheduler = new SchedulerAsServer(serverId, hostname, port);
            else if (type == "storage")
            {
                StorageAsServer storage = new StorageAsServer(serverId, hostname, port);
                storagesAsServers.Add(storage);
            }else if (type == "worker")
            {
                WorkerAsServer worker = new WorkerAsServer(serverId, hostname, port);
                workersAsServers.Add(worker);
            }

        }
        internal void ExecuteCommand(string commandLine)
        {
            Thread t;
            string[] buffer = commandLine.Split(' ');
            switch (buffer[0])
            {
                case "client":
                    t = new Thread(new ThreadStart(() => this.SendAppDataToScheduler(buffer)));
                    t.Start();
                    break;
                case "populate":
                    t = new Thread(new ThreadStart(() => this.StartPopulateStoragesOperation(buffer[1])));
                    t.Start();
                    break;
                case "status":
                    t = new Thread(new ThreadStart(() => this.StartStatusOperation()));
                    t.Start();
                    break;
                case "listServer":
                    t = new Thread(new ThreadStart(() => this.StartListServerOperation(buffer[1])));
                    t.Start();
                    break;
                case "listGlobal":
                    t = new Thread(new ThreadStart(() => this.StartListGlobalOperation()));
                    t.Start();
                    break;
                case "crash":
                    t = new Thread(new ThreadStart(() => this.StartCrashOperation(buffer[1])));
                    t.Start();
                    break;
                default:
                    break;
            }

        }

        private void StartCrashOperation(string serverName)
        {
           foreach(ProcessInfo p in processesInfo)
           {
              if(p.serverName == serverName)
              {
                    Process chosen = Process.GetProcessById(p.processId);
                    chosen.Kill(false);
                    return;
              }
           }
        }

        private void StartListGlobalOperation()
        {
            WriteOnDebugTextBox("------------ LISTGLOBAL -----------");
            //Wirte PM data
            WriteOnDebugTextBox(this.ToString());
            //Write Sched data
            WriteOnDebugTextBox(scheduler.SendListServerRequest());
            //Write Workers data
            foreach (WorkerAsServer work in workersAsServers)
            {
                WriteOnDebugTextBox(work.SendListServerRequest());
            }
            //Write Storages data
            foreach (StorageAsServer stor in storagesAsServers)
            {
                WriteOnDebugTextBox(stor.SendListServerRequest());
            }
        }

        private void StartListServerOperation(string serverName)
        {
            WriteOnDebugTextBox("------------ LISTSERVER ------------");

            if (serverName == scheduler.serverId) {
                WriteOnDebugTextBox(scheduler.SendListServerRequest());
                return;
            }

            foreach(WorkerAsServer work in workersAsServers)
            {
                if(serverName == work.serverId) {
                    WriteOnDebugTextBox(work.SendListServerRequest());
                    return;
                }
            }
            foreach (StorageAsServer stor in storagesAsServers)
            {
                if (serverName == stor.serverId)
                {
                    WriteOnDebugTextBox(stor.SendListServerRequest());
                    return;
                }
            }

            WriteOnDebugTextBox(serverName + " not found!");
        }

        private void WriteOnDebugTextBox(string line)
        {
            guiWindow.BeginInvoke(new DelAddMsg(guiWindow.WriteOnDebugTextBox), new object[] {line});
        }

        private async void StartStatusOperation()
        {
            Task<bool> schedulerTask = Task.Run(() => scheduler.SendStatusRequest());
            List<Task<bool>> workersTask = new List<Task<bool>>();
            foreach(WorkerAsServer worker in workersAsServers)
            {
                Task<bool> workerTask = Task.Run(() => worker.SendStatusRequest());
                workersTask.Add(workerTask);

            }
            List<Task<bool>> storagesTask = new List<Task<bool>>();
            foreach (StorageAsServer storage in storagesAsServers)
            {
                Task<bool> storageTask = Task.Run(() => storage.SendStatusRequest());
                storagesTask.Add(storageTask);

            }
            
            WriteOnDebugTextBox("-------------- STATUS --------------");
            bool schedulerAlive = await schedulerTask;
            if (!schedulerAlive) {/*Write in the DebugTextBox*/
                WriteOnDebugTextBox("Scheduler: " + schedulerMap[0] + " is probably dead!");
            }

            int index = 0;
            foreach (Task<bool> workerTask in workersTask)
            {
                bool workerAlive = await workerTask;
                if (!workerAlive) {/*Write in the DebugTextBox*/
                    WriteOnDebugTextBox("Worker: " + workerMap[index] + " is probably dead!");
                }
                index++;
            }

            index = 0;
            foreach (Task<bool> storageTask in storagesTask)
            {
                bool storageAlive = await storageTask;
                if (!storageAlive) {/*Write in the DebugTextBox*/
                    WriteOnDebugTextBox("Storage: " + storageMap[index] + " is probably dead!");
                }
                index++;
            }

            //Console.WriteLine(schedulerAlive); probably breakpoint here if some errors pop up
        }

        public Boolean SendAppDataToScheduler(string[] buffer)
        {
            /*while(scheduler == null) {
                Task.Delay(100);
            }*/
            return scheduler.SendAppData(buffer);

        }

        //Sync function
        internal void CreateAllConfigEvents(string[] scheduler, string[][] workers, string[][] storages)
        {
            //Save nodes data
            schedulerMap.Add(scheduler[1]);
            foreach (string[] worker in workers)
            {
                workerMap.Add(worker[1]);
            }
            foreach (string[] storage in storages)
            {
                storageMap.Add(storage[1]);
            }
            //Create Storages
            foreach (string[] storage in storages)
            {
                int replicaId = storageMap.BinarySearch(storage[1]);
                int processId = pcs.SendCreateStorageRequest(replicaId, storage, storages);
                processesInfo.Add(new ProcessInfo(storage[1],processId));
                this.CreateChannelWithServer(storage[1], storage[2], "storage");
            }

            //Create Workers
            foreach (string[] worker in workers)
            {
                int replicaId = workerMap.BinarySearch(worker[1]);
                int processId = pcs.SendCreateWorkerRequest(replicaId, worker, storages, storageMap);
                processesInfo.Add(new ProcessInfo(worker[1], processId));
                this.CreateChannelWithServer(worker[1], worker[2], "worker");
            }

            //Create Scheduler
            int pId = pcs.SendCreateSchedulerRequest(schedulerMap.BinarySearch(scheduler[1]), scheduler, workers, workerMap);
            processesInfo.Add(new ProcessInfo(scheduler[1], pId));
            this.CreateChannelWithServer(scheduler[1], scheduler[2], "scheduler");

            //If debugMode == true we need to start connections with worker as client


        }

        internal void StartPopulateStoragesOperation(string storageDataFileName)
        {
            foreach (string line in System.IO.File.ReadLines(storageDataFileName))
            {
                string[] data = line.Split(',');
                this.SendDataToStorage(data[0],data[1]);
            }
        }

        private void SendDataToStorage(string key, string value)
        {
            //Calculate which storage needs to receive the Data TODO
            storagesAsServers[0].SendWriteRequest(key, value);

        }
    }
}
