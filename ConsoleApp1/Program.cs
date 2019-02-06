using System;
using System.Text;
using System.Threading;
using Vostok.Logging.Console;
using org.apache.zookeeper;

namespace ConsoleApp1
{
    class Program
    {
        static void Main(string[] args)
        {
            var client = new org.apache.zookeeper.ZooKeeper(
                "10.217.9.184:2181,10.217.6.124:2181,10.217.6.140:2181,10.217.6.222:2181,10.217.9.47:2181",
                5000,
                null);
            Thread.Sleep(1000);
            var path = "/LinuxMustDie";
            var data = "LINUX IS ALIVE!!";
            var createResult = client.createAsync(path, new byte[0], ZooDefs.Ids.OPEN_ACL_UNSAFE, CreateMode.PERSISTENT);
            Console.WriteLine($"Create result = {createResult}");

            var setDataResult = client.setDataAsync(path, Encoding.UTF8.GetBytes(data)).GetAwaiter().GetResult();
            Console.WriteLine($"SetData result version = {setDataResult.getVersion()}");

            
            var getDataResult = client.getDataAsync(path).GetAwaiter().GetResult();
            Console.WriteLine($"GetData result = {Encoding.UTF8.GetString(getDataResult.Data)}");

            Console.ReadKey();
            client.deleteAsync(path).Wait();
        }
    }
}
