using System;

namespace DIDAStorage {
	public interface IDIDAStorage {
		DIDARecord read(string id, DIDAVersion version);
		DIDAVersion write(string id, string val);
		DIDAVersion updateIfValueIs(string id, string oldvalue, string newvalue);
	}
	public struct DIDARecord {
		public string id;
		public DIDAVersion version;
		public string val;
	}


	public struct DIDAVersion {
		public int versionNumber;
		public int replicaId;
	}
}
