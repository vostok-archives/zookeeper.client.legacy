using System;
using System.Threading;
using Vostok.Logging.Console;
using Vostok.Zookeeper.Client;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            var zk = new ZooKeeperClient("10.217.9.184:2181,10.217.6.124:2181,10.217.6.140:2181,10.217.6.222:2181,10.217.9.47:2181", TimeSpan.FromSeconds(5), new ConsoleLog());
            zk.Start();
            Thread.Sleep(1000);

            zk.Delete("/nesting1", deleteChildrenIfNeeded: true);
            zk.Delete("/node", deleteChildrenIfNeeded: true);
            zk.Delete("/persistentNode", deleteChildrenIfNeeded: true);
            zk.Delete("/ephemeral", deleteChildrenIfNeeded: true);
            zk.Delete("/getChildrenEphemeral", deleteChildrenIfNeeded: true);
            zk.Delete("/getChildrenPersistent", deleteChildrenIfNeeded: true);
            zk.Delete("/getChildrenWithStatEphemeral", deleteChildrenIfNeeded: true);
            zk.Delete("/getChildrenWithStatPersistent", deleteChildrenIfNeeded: true);
            zk.Delete("/forDelete", deleteChildrenIfNeeded: true); 
            zk.Delete("/setData", deleteChildrenIfNeeded: true); 

        }
    }
}
