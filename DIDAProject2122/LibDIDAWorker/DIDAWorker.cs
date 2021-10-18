using System;

namespace DIDAWorker {

	public enum OperationType { ReadOp, WriteOp, UpdateIfOp};

	public delegate DIDAStorageNode delLocateStorageId(string id, OperationType type);
	public interface IDIDAOperator {
		// the meta-record, the input and the output of the previous operator are available in the DIDARequest
		// the return value is this Operator return value which the worker should add to the DIDARequest before 
		// forwarding it to the next worker/operator.
		public string ProcessRecord(DIDAMetaRecord meta, string input, string previousOperatorOutput);
		public void ConfigureStorage(DIDAStorageNode[] storageReplicas, delLocateStorageId locationFunction);
		// the location function is passed to the operator so it may know in which storage node to do an operation
		// based on the record id and the operation type.
	}


	public struct DIDARequest {
		public DIDAMetaRecord meta;
		public string input;
		public int next;
		public int chainSize;
		public DIDAAssignment[] chain;
	}


	public struct DIDAAssignment {
		public DIDAOperatorID op;
		public string host;
		public int port;
		public string output;
	}


	public class DIDAMetaRecord {
		public int id;
	}

	public struct DIDAStorageNode {
		public string serverId;
		public string host;
		public int port;
	}

	public struct DIDAOperatorID {
		public string classname;
		public int order;
}

}
