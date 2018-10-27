using org.apache.curator.ensemble;

namespace Vostok.Zookeeper.Client.Utilities
{
    internal interface IRandomizedEnsembleProvider : EnsembleProvider
    {
        void Randomize();
    }
}