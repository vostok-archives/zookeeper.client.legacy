using System;

namespace Vostok.Zookeeper.Client
{
    public class AnonymousWatcher : IWatcher
    {
        private readonly Action<EventType, string> processingDelegate;

        public AnonymousWatcher(Action<EventType, string> processingDelegate)
        {
            this.processingDelegate = processingDelegate;
        }

        public void ProcessEvent(EventType type, string path)
        {
            processingDelegate(type, path);
        }
    }
}