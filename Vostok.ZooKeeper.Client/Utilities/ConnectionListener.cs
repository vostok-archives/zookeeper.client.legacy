using System;
using org.apache.curator.framework;
using org.apache.curator.framework.state;

namespace Vostok.Zookeeper.Client.Utilities
{
    internal class ConnectionListener : ConnectionStateListener
    {
        private readonly Action<ConnectionState> callback;

        public ConnectionListener(Action<ConnectionState> callback)
        {
            this.callback = callback;
        }

        public void stateChanged(CuratorFramework curator, org.apache.curator.framework.state.ConnectionState state)
        {
            callback((ConnectionState) state.ordinal());
        }
    }
}