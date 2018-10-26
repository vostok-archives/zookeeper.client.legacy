namespace Vostok.Zookeeper.Client.Utilities
{
    internal interface IRandomizedEnsembleProvider : org.apache.curator.ensemble.EnsembleProvider
    {
        void Randomize();
    }
}