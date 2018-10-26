using System;

namespace Vostok.Zookeeper.Client
{
    public class AnonymousWatcher : IWatcher
    {
        public AnonymousWatcher(Action<EventType, string> processingDelegate)
        {
            this.processingDelegate = processingDelegate;
        }

        public void ProcessEvent(EventType type, string path)
        {
            processingDelegate(type, path);
        }

        private readonly Action<EventType, string> processingDelegate;
    }
}