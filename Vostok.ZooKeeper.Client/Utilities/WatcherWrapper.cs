using org.apache.curator.framework.api;
using org.apache.zookeeper;

namespace Vostok.Zookeeper.Client.Utilities
{
	internal class WatcherWrapper : CuratorWatcher
	{
		public WatcherWrapper(IWatcher userWatcher)
		{
			this.userWatcher = userWatcher;
		}

		public void process(WatchedEvent watcherEvent)
		{
			if (!watcherEvent.getType().Equals(Watcher.Event.EventType.None))
				userWatcher.ProcessEvent((EventType) watcherEvent.getType().getIntValue(), watcherEvent.getPath());
		}

		private readonly IWatcher userWatcher;
	}
}