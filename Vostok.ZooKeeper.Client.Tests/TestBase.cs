using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using FluentAssertions.Extensions;
using NUnit.Framework;
using org.apache.zookeeper;
using org.apache.zookeeper.data;
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
            ensemble = ZooKeeperEnsemble.DeployNew(1, log);
        }

        [OneTimeTearDown]
        public void OneTimeTearDownBase()
        {
            ensemble.Dispose();
        }

        protected static void EnsureChildrenExistWithCorrectStat(org.apache.zookeeper.ZooKeeper client, string rootNode, string[] children, int nodeVersion = 0, int childVersion = 0)
        {
            var getChildrenWithStatResult = client.getChildrenAsync(rootNode).GetAwaiter().GetResult();
            getChildrenWithStatResult.Children.Should().BeEquivalentTo(children);
            //getChildrenWithStatResult.Path.Should().Be(rootNode);
            //getChildrenWithStatResult.Status.Should().Be(ZooKeeperStatus.Ok);
            //getChildrenWithStatResult.Payload.Item1.Should().BeEquivalentTo(children);
            //getChildrenWithStatResult.Payload.Item2.Version.Should().Be(nodeVersion);
            //getChildrenWithStatResult.Payload.Item2.Cversion.Should().Be(childVersion);
        }

        protected static void EnsureChildrenExists(org.apache.zookeeper.ZooKeeper client, string rootNode, string[] children)
        {
            var getChildrenResult = client.getChildrenAsync(rootNode).GetAwaiter().GetResult();
            getChildrenResult.Children.Should().BeEquivalentTo(children);
            //getChildrenResult.EnsureSuccess();
            //getChildrenResult.Status.Should().Be(ZooKeeperStatus.Ok);
            //getChildrenResult.Path.Should().Be(rootNode);
            //getChildrenResult.Payload.Should().BeEquivalentTo(children);
        }

        protected static void CreateNode(string path, org.apache.zookeeper.CreateMode createMode, org.apache.zookeeper.ZooKeeper client)
        {
            var parts = path.Split(new[] {"/"}, StringSplitOptions.RemoveEmptyEntries);
            var currentPart = string.Empty;
            foreach (var part in parts)
            {
                currentPart = $"{currentPart}/{part}";
                var createResult = client.createAsync(currentPart, new byte[0], ZooDefs.Ids.OPEN_ACL_UNSAFE, createMode).Result;
                createResult.Should().NotBeNull();
            }
        }

        protected static void EnsureNodeExist(string path, org.apache.zookeeper.ZooKeeper anotherClient, int expectedVersion = 0)
        {
            var existResult = anotherClient.existsAsync(path).GetAwaiter().GetResult();
            existResult.Should().NotBeNull();
            //existResult.EnsureSuccess();
            //existResult.Path.Should().Be(path);
            //existResult.Status.Should().Be(ZooKeeperStatus.Ok);
            //existResult.Payload.Version.Should().Be(expectedVersion);
        }

        protected static void EnsureNodeDoesNotExist(string path, org.apache.zookeeper.ZooKeeper anotherClient)
        {
            var existResult = anotherClient.existsAsync(path).GetAwaiter().GetResult();
            existResult.Should().BeNull();
            //existResult.EnsureSuccess();
            //existResult.Path.Should().Be(path);
            //existResult.Status.Should().Be(ZooKeeperStatus.Ok);
            //existResult.Payload.Should().BeNull();
        }

        protected static void DeleteNode(string path, org.apache.zookeeper.ZooKeeper client)
        {
            client.deleteAsync(path).Wait();
        }

        protected static void DeleteNonexistentNode(string path, org.apache.zookeeper.ZooKeeper client)
        {
            try
            {
                client.deleteAsync(path).GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                e.Should().BeAssignableTo<KeeperException.NoNodeException>();
            }
            //deleteResult.Status.Should().Be(ZooKeeperStatus.NoNode);
            //deleteResult.Path.Should().Be(path);
        }

        protected static void SetData(string path, string data, org.apache.zookeeper.ZooKeeper client)
        {
            var setDataResult = client.setDataAsync(path, Encoding.UTF8.GetBytes(data)).GetAwaiter().GetResult();
            setDataResult.Should().NotBeNull();
            //setDataResult.EnsureSuccess();
            //setDataResult.Status.Should().Be(ZooKeeperStatus.Ok);
            //setDataResult.Path.Should().Be(path);
        }

        protected static void EnsureDataExists(string path, org.apache.zookeeper.ZooKeeper client, string expectedData, int expectedVersion = 0)
        {
            var expectedDataBytes = Encoding.UTF8.GetBytes(expectedData);
            var getDataResult = client.getDataAsync(path).GetAwaiter().GetResult();
            getDataResult.Data.Should().BeEquivalentTo(expectedDataBytes);
            //getDataResult.EnsureSuccess();
            //getDataResult.Path.Should().Be(path);
            //getDataResult.Status.Should().Be(ZooKeeperStatus.Ok);
            //getDataResult.Payload.Item1.Should().BeEquivalentTo(expectedDataBytes);
            //getDataResult.Payload.Item2.Version.Should().Be(expectedVersion);
        }

        protected static void CheckVersions(org.apache.zookeeper.ZooKeeper client, string rootNode, int version, int cVersion)
        {
            var currentStat = client.existsAsync(rootNode).GetAwaiter().GetResult();
            currentStat.getVersion().Should().Be(version);
            currentStat.getCversion().Should().Be(cVersion);
            //currentStat.Version.Should().Be(version);
            //currentStat.Cversion.Should().Be(cVersion);
        }

        protected org.apache.zookeeper.ZooKeeper CreateNewClient(ILog logForClient = null)
        {
            //10.217.9.184:2181,10.217.6.124:2181,10.217.6.140:2181,10.217.6.222:2181,10.217.9.47:2181
            var client = new org.apache.zookeeper.ZooKeeper(
                //"10.217.9.184:2181,10.217.6.124:2181,10.217.6.140:2181,10.217.6.222:2181,10.217.9.47:2181",
                ensemble.ConnectionString,
                5000,
                null);
            Thread.Sleep(1.Seconds());
            return client;
        }
    }


    internal class WatcherDelegate : Watcher
    {
        private readonly Func<WatchedEvent, Task> processor;

        public WatcherDelegate(Func<WatchedEvent, Task> processor)
        {
            this.processor = processor;
        }

        public override Task process(WatchedEvent @event)
        {
            return this.processor(@event);
        }
    }
}