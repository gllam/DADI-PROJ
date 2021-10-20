using DIDAWorker;
using Grpc.Core;
using Grpc.Net.Client;

namespace Scheduler
{

    public class SchedulerService : DIDASchedulerService.DIDASchedulerServiceBase
    {
        Dictionary<string, GrpcChannel> _workerChannels = new Dictionary<string, GrpcChannel>();

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
            DIDARequest requestApp = new DIDARequest();
            requestApp.chainSize = request.App.Count();
            requestApp.chain = new DIDAAssignment[requestApp.chainSize];
            for (int opIndex = 0; opIndex < requestApp.chainSize; opIndex++)
            {
                requestApp.chain[opIndex] = new DIDAAssignment();
            }
            return new SendScriptReply
            {
                Ack = true
            };
        }

        /*
        public void setWorkers(List<string> app)
        {
            foreach (string worker in workers)
            {

            }
        }
        */
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.ReadKey();
        }
    }
}
