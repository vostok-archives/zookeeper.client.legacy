using System;
using System.Linq;
using FluentAssertions;
using FluentAssertions.Extensions;
using NUnit.Framework;
using Vostok.Logging.Abstractions;
using Vostok.Commons.Testing;

namespace Vostok.Zookeeper.Client.Tests
{
    [TestFixture]
    internal class ZooKeeperClient_Tests : TestBase
    {
        public ZooKeeperClient_Tests()
            : base(new SilentLog())
        {
        }

        [Test]
        public void IsStarted_should_be_false_by_default()
        {
            var client = new ZooKeeperClient(ensemble.ConnectionString, 5.Seconds(), new SilentLog());

            client.IsStarted.Should().BeFalse();
            client.IsConnected.Should().BeFalse();
        }

        [Test]
        public void Start_should_start_client()
        {
            var client = new ZooKeeperClient(ensemble.ConnectionString, 5.Seconds(), new SilentLog());

            client.Start();
            client.IsStarted.Should().BeTrue();
            Action checkIsConnected = () => client.IsConnected.Should().BeTrue();
            checkIsConnected.ShouldPassIn(5.Seconds());
        }

        [Test]
        public void Test_KillSession()
        {
            using (var client = CreateNewClient())
            {
                var sessionId = client.SessionId;
                var sessionPassword = client.SessionPassword;

                client.KillSession(TimeSpan.FromSeconds(10));
                client.WaitUntilConnected();

                Action checkNewSessionIdAndPassword = () =>
                {
                    // ReSharper disable AccessToDisposedClosure
                    client.SessionId.Should().NotBe(sessionId);
                    client.SessionPassword.Should().NotBeEquivalentTo(sessionPassword);
                };
                checkNewSessionIdAndPassword.ShouldPassIn(10.Seconds());
            }
        }

        [TestCase("/nesting1")]
        [TestCase("/node/nesting2")]
        [TestCase("/node/nesting2_1")]
        [TestCase("/node/nesting2/nesting3")]
        public void Create_should_create_persistent_node(string path)
        {
            using (var client = CreateNewClient())
            {
                CreateNode(path, CreateMode.Persistent, client);

                EnsureNodeExist(path, client);

                DeleteNode(path, client);
            }
        }

        [TestCase("/persistentNode")]
        public void Created_persistent_node_should_be_alive_after_client_stop(string path)
        {
            using (var client = CreateNewClient())
            {
                CreateNode(path, CreateMode.Persistent, client);
            }

            using (var anotherClient = CreateNewClient())
            {
                EnsureNodeExist(path, anotherClient);

                anotherClient.Create(path, null, CreateMode.Persistent).Status.Should().Be(ZooKeeperStatus.NodeExists);

                DeleteNode(path, anotherClient);
            }
        }

        [TestCase("/ephemeral/node1", "/ephemeral/node2")]
        public void Created_ephemeral_node_should_disappear_when_owning_client_Disposes(params string[] nodes)
        {
            var client = CreateNewClient();

            foreach (var node in nodes)
            {
                CreateNode(node, CreateMode.Ephemeral, client);
            }

            client.Dispose();

            using (var anotherClient = CreateNewClient())
            {
                foreach (var node in nodes)
                {
                    EnsureNodeDoesNotExist(node, anotherClient);
                }
            }
        }

        [TestCase("/forDelete/node1", CreateMode.Ephemeral)]
        [TestCase("/forDelete/node2", CreateMode.Persistent)]
        public void Delete_should_delete_node(string path, CreateMode createMode)
        {
            using (var client = CreateNewClient())
            {
                CreateNode(path, createMode, client);

                DeleteNode(path, client);

                EnsureNodeDoesNotExist(path, client);
            }

            using (var anotherClient = CreateNewClient())
            {
                EnsureNodeDoesNotExist(path, anotherClient);
            }
        }

        [TestCase("/forDelete/nonexistent/node")]
        public void Delete_nonexistent_node_should_return_NoNode(string path)
        {
            using (var client = CreateNewClient())
            {
                DeleteNonexistentNode(path, client);

                EnsureNodeDoesNotExist(path, client);
            }

            using (var anotherClient = CreateNewClient())
            {
                EnsureNodeDoesNotExist(path, anotherClient);
            }
        }

        [TestCase("/setData/node/qwerty", "qwerty")]
        public void SetData_and_GetData_should_works_correct(string path, string data)
        {
            using (var client = CreateNewClient())
            {
                CreateNode(path, CreateMode.Persistent, client);

                SetData(path, data, client);

                EnsureDataExists(path, client, data, 1);

                DeleteNode(path, client);
            }
        }

        [TestCase(CreateMode.Ephemeral, "/getChildrenEphemeral/child1", "/getChildrenEphemeral/child2", "/getChildrenEphemeral/child3")]
        [TestCase(CreateMode.Persistent, "/getChildrenPersistent/child1", "/getChildrenPersistent/child2", "/getChildrenPersistent/child3")]
        public void GetChildren_should_return_all_children(CreateMode createMode, params string[] nodes)
        {
            var rootNode = "/" + nodes.First().Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries).First();
            var children = nodes.Select(x => x.Replace(rootNode + "/", string.Empty)).ToArray();

            using (var client = CreateNewClient())
            {
                foreach (var node in nodes)
                {
                    CreateNode(node, createMode, client);
                }
                
                EnsureChildrenExists(client, rootNode, children);
            }

            if (createMode == CreateMode.Ephemeral)
                return;

            using (var anotherClient = CreateNewClient())
            {
                EnsureChildrenExists(anotherClient, rootNode, children);

                foreach (var node in nodes)
                {
                    DeleteNode(node, anotherClient);
                }
            }
        }

        [TestCase(CreateMode.Ephemeral, "/getChildrenWithStatEphemeral/child1", "/getChildrenWithStatEphemeral/child2", "/getChildrenWithStatEphemeral/child3")]
        [TestCase(CreateMode.Persistent, "/getChildrenWithStatPersistent/child1", "/getChildrenWithStatPersistent/child2", "/getChildrenWithStatPersistent/child3")]
        public void GetChildrenWithStat_should_return_all_children_with_correct_Stat(CreateMode createMode, params string[] nodes)
        {
            var rootNode = "/" + nodes.First().Split(new[] { "/" }, StringSplitOptions.RemoveEmptyEntries).First();
            var children = nodes.Select(x => x.Replace(rootNode + "/", string.Empty)).ToArray();

            using (var client = CreateNewClient())
            {
                foreach (var node in nodes)
                {
                    CreateNode(node, createMode, client);
                }

                EnsureChildrenExistWithCorrectStat(client, rootNode, children, 0, 3);
            }

            if (createMode == CreateMode.Ephemeral)
                return;

            using (var anotherClient = CreateNewClient())
            {
                EnsureChildrenExistWithCorrectStat(anotherClient, rootNode, children, 0, 3);

                foreach (var node in nodes)
                {
                    DeleteNode(node, anotherClient);
                }
            }
        }

        [Test]
        public void Client_should_correct_see_node_Stat()
        {
            const string rootNode = "/statChecking";
            const string childNode = "/statChecking/child1";

            using (var client = CreateNewClient())
            {
                CreateNode(rootNode, CreateMode.Persistent, client);

                var currentVersion = 0;
                var currentCVersion = 0;
                CheckVersions(client, rootNode, currentVersion, currentCVersion);

                for (var i = 0; i < 3; i++)
                {
                    CreateNode(childNode, CreateMode.Persistent, client);
                    currentCVersion++;

                    CheckVersions(client, rootNode, currentVersion, currentCVersion);

                    SetData(rootNode, "some data", client);
                    currentVersion++;

                    CheckVersions(client, rootNode, currentVersion, currentCVersion);

                    DeleteNode(childNode, client);
                    currentCVersion++;

                    CheckVersions(client, rootNode, currentVersion, currentCVersion);
                }

                DeleteNode(rootNode, client);
            }
        }
    }
}
