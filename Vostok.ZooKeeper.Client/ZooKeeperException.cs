using System;

namespace Vostok.Zookeeper.Client
{
	public class ZooKeeperException : Exception
	{
		public ZooKeeperException(ZooKeeperStatus status, string path)
			: base (String.Format("ZooKeeper operation has failed with status '{0}' for path '{1}'.", status, path)) { }
	}
}