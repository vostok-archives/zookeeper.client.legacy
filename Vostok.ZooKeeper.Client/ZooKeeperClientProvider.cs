using System;
using System.Collections.Concurrent;
using Vostok.Commons.Time;
using Vostok.Logging.Abstractions;

namespace Vostok.Zookeeper.Client
{
    public static class ZooKeeperClientProvider
    {
        private static readonly TimeSpan DefaultSessionTimeout = 10.Seconds();

        // mapping: connection string --> client
        private static readonly ConcurrentDictionary<string, ZooKeeperClient> Clients;

        static ZooKeeperClientProvider()
        {
            Clients = new ConcurrentDictionary<string, ZooKeeperClient>();
        }

        public static ZooKeeperClient GetClient(string connectionString, ILog log)
        {
            var client = Clients.GetOrAdd(connectionString ?? string.Empty, key => new ZooKeeperClient(connectionString, DefaultSessionTimeout, log));
            client.Start();

            return client;
        }
    }
}