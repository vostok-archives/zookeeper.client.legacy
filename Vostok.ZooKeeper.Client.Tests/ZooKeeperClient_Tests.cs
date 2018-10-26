using System.Text;
using FluentAssertions;
using NUnit.Framework;
using Vostok.Logging.Abstractions;

namespace Vostok.Zookeeper.Client.Tests
{
    [TestFixture]
    internal class ZooKeeperClient_Tests : TestBase
    {
        public ZooKeeperClient_Tests()
            : base(new SilentLog())
        {
        }

        [TestCase("/nesting1")]
        [TestCase("/node/nesting2")]
        [TestCase("/node/nesting2_1")]
        [TestCase("/node/nesting2/nesting3")]
        public void Create_should_create_persistent_node(string path)
        {
            using (var client = CreateNewClient())
            {
                var createResult = client.Create(path, null, CreateMode.Persistent).EnsureSuccess();

                createResult.Status.Should().Be(ZooKeeperStatus.Ok);
                createResult.Path.Should().Be(path);
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
        public void SetData_should(string path, string data)
        {
            using (var client = CreateNewClient())
            {
                CreateNode(path, CreateMode.Persistent, client);

                SetData(path, data, client);

                EnsureDataExists(path, client, data, 1);
            }
        }

        private static void CreateNode(string path, CreateMode createMode, IZooKeeperClient client)
        {
            var createResult = client.Create(path, null, createMode).EnsureSuccess();
            createResult.Path.Should().Be(path);
            createResult.Status.Should().Be(ZooKeeperStatus.Ok);
        }

        private static void EnsureNodeExist(string path, IZooKeeperClient anotherClient, int expectedVersion = 0)
        {
            var existResult = anotherClient.Exists(path);
            existResult.Path.Should().Be(path);
            existResult.Status.Should().Be(ZooKeeperStatus.Ok);
            existResult.Payload.Version.Should().Be(expectedVersion);
        }

        private static void EnsureNodeDoesNotExist(string path, IZooKeeperClient anotherClient)
        {
            var existResult = anotherClient.Exists(path);
            existResult.Path.Should().Be(path);
            existResult.Status.Should().Be(ZooKeeperStatus.Ok);
            existResult.Payload.Should().BeNull();
        }

        private static void DeleteNode(string path, IZooKeeperClient client)
        {
            var deleteResult = client.Delete(path).EnsureSuccess();
            deleteResult.Status.Should().Be(ZooKeeperStatus.Ok);
            deleteResult.Path.Should().Be(path);
        }

        private static void DeleteNonexistentNode(string path, IZooKeeperClient client)
        {
            var deleteResult = client.Delete(path);
            deleteResult.Status.Should().Be(ZooKeeperStatus.NoNode);
            deleteResult.Path.Should().Be(path);
        }

        private static void SetData(string path, string data, IZooKeeperClient client)
        {
            var setDataResult = client.SetData(path, Encoding.UTF8.GetBytes(data)).EnsureSuccess();
            setDataResult.Status.Should().Be(ZooKeeperStatus.Ok);
            setDataResult.Path.Should().Be(path);
        }

        private static void EnsureDataExists(string path, IZooKeeperClient client, string expectedData, int expectedVersion = 0)
        {
            var expectedDataBytes = Encoding.UTF8.GetBytes(expectedData);
            var getDataResult = client.GetData(path);
            getDataResult.EnsureSuccess();
            getDataResult.Path.Should().Be(path);
            getDataResult.Status.Should().Be(ZooKeeperStatus.Ok);
            getDataResult.Payload.Item1.Should().BeEquivalentTo(expectedDataBytes);
            getDataResult.Payload.Item2.Version.Should().Be(expectedVersion);
        }
    }
}
