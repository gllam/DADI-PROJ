using System;
using DIDAOperator;
using DIDAWorker;

namespace OperatorRunner {
    class Program {
        static void Main(string[] args) {
            IDIDAOperator op = new CounterOperator();
            DIDAMetaRecord meta = new DIDAMetaRecord { id = 1 };
            op.ConfigureStorage(new DIDAStorageNode[] { new DIDAStorageNode { host = "localhost", port=2001, serverId = "s1"} }, MyLocationFunction);
            string result = op.ProcessRecord(meta, "sample_input", "sample_previous_output");
            Console.WriteLine("result: " + result);
            Console.ReadLine();
        
        }

        private static DIDAStorageNode MyLocationFunction(string id, OperationType type) {
            return new DIDAStorageNode { host = "localhost", port = 2001, serverId = "s1" };
        }
    }
}
