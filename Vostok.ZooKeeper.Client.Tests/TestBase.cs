using FluentAssertions.Extensions;
using NUnit.Framework;
using Vostok.Logging.Abstractions;
using Vostok.ZooKeeper.LocalEnsemble;

namespace Vostok.Zookeeper.Client.Tests
{
    internal abstract class TestBase
    {
        private readonly ILog log;
        protected ZooKeeperEnsemble ensemble;

        protected TestBase(ILog log)
        {
            this.log = log;
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            ensemble = ZooKeeperEnsemble.DeployNew(5, log);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            ensemble.Dispose();
        }

        protected IZooKeeperClient CreateNewClient(ILog logForClient = null)
        {
            var client = new ZooKeeperClient(ensemble.ConnectionString, 5.Seconds(), logForClient ?? log);
            client.Start();
            return client;
        }
    }
}
