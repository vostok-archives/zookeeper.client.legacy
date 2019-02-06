using System.Text;
using FluentAssertions;
using FluentAssertions.Extensions;
using NUnit.Framework;
using Vostok.Logging.Abstractions;
using Vostok.ZooKeeper.LocalEnsemble;

namespace Vostok.Zookeeper.Client.Tests
{
    internal abstract class TestBase
    {
        protected ZooKeeperEnsemble ensemble;
        private readonly ILog log;

        protected TestBase(ILog log)
        {
            this.log = log;
        }

        [OneTimeSetUp]
        public void OneTimeSetUpBase()
        {
            //ensemble = ZooKeeperEnsemble.DeployNew(1, log);
        }

        [OneTimeTearDown]
        public void OneTimeTearDownBase()
        {
            //ensemble.Dispose();
        }

        protected static void EnsureChildrenExistWithCorrectStat(IZooKeeperClient client, string rootNode, string[] children, int nodeVersion = 0, int childVersion = 0)
        {
            var getChildrenWithStatResult = client.GetChildrenWithStat(rootNode);
            getChildrenWithStatResult.Path.Should().Be(rootNode);
            getChildrenWithStatResult.Status.Should().Be(ZooKeeperStatus.Ok);
            getChildrenWithStatResult.Payload.Item1.Should().BeEquivalentTo(children);
            getChildrenWithStatResult.Payload.Item2.Version.Should().Be(nodeVersion);
            getChildrenWithStatResult.Payload.Item2.Cversion.Should().Be(childVersion);
        }

        protected static void EnsureChildrenExists(IZooKeeperClient client, string rootNode, string[] children)
        {
            var getChildrenResult = client.GetChildren(rootNode);
            getChildrenResult.EnsureSuccess();
            getChildrenResult.Status.Should().Be(ZooKeeperStatus.Ok);
            getChildrenResult.Path.Should().Be(rootNode);
            getChildrenResult.Payload.Should().BeEquivalentTo(children);
        }

        protected static void CreateNode(string path, CreateMode createMode, IZooKeeperClient client)
        {
            var createResult = client.Create(path, null, createMode).EnsureSuccess();
            createResult.Path.Should().Be(path);
            createResult.Status.Should().Be(ZooKeeperStatus.Ok);
        }

        protected static void EnsureNodeExist(string path, IZooKeeperClient anotherClient, int expectedVersion = 0)
        {
            var existResult = anotherClient.Exists(path);
            existResult.EnsureSuccess();
            existResult.Path.Should().Be(path);
            existResult.Status.Should().Be(ZooKeeperStatus.Ok);
            existResult.Payload.Version.Should().Be(expectedVersion);
        }

        protected static void EnsureNodeDoesNotExist(string path, IZooKeeperClient anotherClient)
        {
            var existResult = anotherClient.Exists(path);
            existResult.EnsureSuccess();
            existResult.Path.Should().Be(path);
            existResult.Status.Should().Be(ZooKeeperStatus.Ok);
            existResult.Payload.Should().BeNull();
        }

        protected static void DeleteNode(string path, IZooKeeperClient client)
        {
            var deleteResult = client.Delete(path).EnsureSuccess();
            deleteResult.Status.Should().Be(ZooKeeperStatus.Ok);
            deleteResult.Path.Should().Be(path);
        }

        protected static void DeleteNonexistentNode(string path, IZooKeeperClient client)
        {
            var deleteResult = client.Delete(path);
            deleteResult.Status.Should().Be(ZooKeeperStatus.NoNode);
            deleteResult.Path.Should().Be(path);
        }

        protected static void SetData(string path, string data, IZooKeeperClient client)
        {
            var setDataResult = client.SetData(path, Encoding.UTF8.GetBytes(data)).EnsureSuccess();
            setDataResult.EnsureSuccess();
            setDataResult.Status.Should().Be(ZooKeeperStatus.Ok);
            setDataResult.Path.Should().Be(path);
        }

        protected static void EnsureDataExists(string path, IZooKeeperClient client, string expectedData, int expectedVersion = 0)
        {
            var expectedDataBytes = Encoding.UTF8.GetBytes(expectedData);
            var getDataResult = client.GetData(path);
            getDataResult.EnsureSuccess();
            getDataResult.Path.Should().Be(path);
            getDataResult.Status.Should().Be(ZooKeeperStatus.Ok);
            getDataResult.Payload.Item1.Should().BeEquivalentTo(expectedDataBytes);
            getDataResult.Payload.Item2.Version.Should().Be(expectedVersion);
        }

        protected static void CheckVersions(IZooKeeperClient client, string rootNode, int version, int cVersion)
        {
            var currentStat = client.GetData(rootNode).Payload.Item2;
            currentStat.Version.Should().Be(version);
            currentStat.Cversion.Should().Be(cVersion);
        }

        protected ZooKeeperClient CreateNewClient(ILog logForClient = null)
        {
            //10.217.9.184:2181,10.217.6.124:2181,10.217.6.140:2181,10.217.6.222:2181,10.217.9.47:2181
            var client = new ZooKeeperClient("10.217.9.184:2181,10.217.6.124:2181,10.217.6.140:2181,10.217.6.222:2181,10.217.9.47:2181", 5.Seconds(), logForClient ?? log);
            //var client = new ZooKeeperClient(ensemble.ConnectionString, 5.Seconds(), logForClient ?? log);
            client.Start();
            client.WaitUntilConnected();
            return client;
        }
    }
}