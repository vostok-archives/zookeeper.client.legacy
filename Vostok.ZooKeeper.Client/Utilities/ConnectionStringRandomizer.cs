using System;
using System.Linq;
using org.apache.curator.ensemble;
using Vostok.Commons.Collections;
using Vostok.Commons.Time;

namespace Vostok.Zookeeper.Client.Utilities
{
    internal class ConnectionStringRandomizer
    {
        public ConnectionStringRandomizer(EnsembleProvider ensembleProvider)
        {
            this.ensembleProvider = ensembleProvider;

            locker = new object();
            lastRandomization = DateTime.MinValue;
            attemptTimestamps = new CircularBuffer<DateTime>(RecentAttemptsNeededToRandomize);
        }

        public bool RandomizeIfNeeded()
        {
            var randomizedProvider = ensembleProvider as IRandomizedEnsembleProvider;
            if (randomizedProvider == null)
                return false;

            lock (locker)
            {
                var currentTimestamp = DateTime.UtcNow;
                var timeSinceLastRandomization = currentTimestamp - lastRandomization;
                if (timeSinceLastRandomization <= 1.Minutes())
                    return false;

                attemptTimestamps.Add(currentTimestamp);

                if (HadMultipleAttemptsRecently(currentTimestamp))
                {
                    randomizedProvider.Randomize();
                    lastRandomization = currentTimestamp;
                    attemptTimestamps.Clear();
                    return true;
                }
            }

            return false;
        }

        private bool HadMultipleAttemptsRecently(DateTime currentTimestamp)
        {
            return 
                attemptTimestamps.Count == attemptTimestamps.Capacity && 
                attemptTimestamps.All(ts => currentTimestamp - ts <= RecencyThreshold);
        }

        private readonly EnsembleProvider ensembleProvider;
        private readonly CircularBuffer<DateTime> attemptTimestamps;
        private readonly object locker;
        private DateTime lastRandomization;

        private const int RecentAttemptsNeededToRandomize = 6;
        private static readonly TimeSpan RecencyThreshold = 10.Minutes();
    }
}