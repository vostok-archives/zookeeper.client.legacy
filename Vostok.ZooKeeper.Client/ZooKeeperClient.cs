using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;
using java.lang;
using java.util;
using org.apache.curator.ensemble;
using org.apache.curator.ensemble.@fixed;
using org.apache.curator.framework;
using org.apache.curator.framework.api;
using org.apache.curator.framework.imps;
using org.apache.zookeeper;
using Vostok.Commons.Time;
using Vostok.Logging.Abstractions;
using Vostok.Zookeeper.Client.Utilities;
using Exception = System.Exception;
using String = System.String;
using Thread = System.Threading.Thread;

namespace Vostok.Zookeeper.Client
{
    public class ZooKeeperClient : IZooKeeperClient
    {
        private const int maxDataLength = 1023 * 1024;

        private readonly ILog baseLog;
        private readonly ConcurrentDictionary<IWatcher, WatcherWrapper> watcherWrappers;
        private readonly ConnectionStringRandomizer connectionStringRandomizer;

        public event Action<ConnectionState> ConnectionStateChanged;

        /// <summary>
        /// Создает экземпляр клиента, используя фиксированную connection string.
        /// </summary>
        /// <param name="connectionString">Имя CC-топологии или список пар host:port, разделенных запятой (например, "zk1:2181,zk2:2181").</param>
        /// <param name="sessionTimeout">Таймаут клиентской сессии. Не рекомендуется делать меньше 5с. или больше минуты.</param>
        /// <param name="retryStrategy">Стратегия повторных попыток в случае потери соединения.</param>
        /// <param name="nameSpace">Chroot для всех путей. Служит для изоляции приложений. Пример: "MyApp".</param>
        /// <param name="log"></param>
        public ZooKeeperClient(string connectionString, TimeSpan sessionTimeout, RetryStrategy retryStrategy, string nameSpace, ILog log)
            : this(CreateEnsembleProvider(connectionString, log), sessionTimeout, retryStrategy, nameSpace, false, log)
        {
        }

        /// <summary>
        /// Создает экземпляр клиента, используя фиксированную connection string.
        /// </summary>
        /// <param name="connectionString">Имя CC-топологии или список  пар host:port, разделенных запятой (например, "zk1:2181,zk2:2181").</param>
        /// <param name="sessionTimeout">Таймаут клиентской сессии. Не рекомендуется делать меньше 5с. или больше минуты.</param>
        /// <param name="retryStrategy">Стратегия повторных попыток в случае потери соединения.</param>
        /// <param name="log"></param>
        public ZooKeeperClient(string connectionString, TimeSpan sessionTimeout, RetryStrategy retryStrategy, ILog log)
            : this(connectionString, sessionTimeout, retryStrategy, null, log)
        {
        }

        /// <summary>
        /// Создает экземпляр клиента, используя фиксированную connection string.
        /// </summary>
        /// <param name="connectionString">Имя CC-топологии или список  пар host:port, разделенных запятой (например, "zk1:2181,zk2:2181").</param>
        /// <param name="sessionTimeout">Таймаут клиентской сессии. Не рекомендуется делать меньше 5с. или больше минуты.</param>
        /// <param name="nameSpace">Chroot для всех путей. Служит для изоляции приложений. Пример: "MyApp".</param>
        /// <param name="log"></param>
        public ZooKeeperClient(string connectionString, TimeSpan sessionTimeout, string nameSpace, ILog log)
            : this(connectionString, sessionTimeout, RetryStrategy.CreateDefault(sessionTimeout), nameSpace, log)
        {
        }

        /// <summary>
        /// Создает экземпляр клиента, используя фиксированную connection string.
        /// </summary>
        /// <param name="connectionString">Имя CC-топологии или список  пар host:port, разделенных запятой (например, "zk1:2181,zk2:2181").</param>
        /// <param name="sessionTimeout">Таймаут клиентской сессии. Не рекомендуется делать меньше 5с. или больше минуты.</param>
        /// <param name="log"></param>
        public ZooKeeperClient(string connectionString, TimeSpan sessionTimeout, ILog log)
            : this(connectionString, sessionTimeout, null as string, log)
        {
        }

        internal ZooKeeperClient(EnsembleProvider ensembleProvider, TimeSpan sessionTimeout, RetryStrategy retryStrategy, string nameSpace, bool canBeReadonly, ILog log)
            : this(
                CuratorFrameworkFactory
                    .builder()
                    .ensembleProvider(ensembleProvider)
                    .sessionTimeoutMs((int) sessionTimeout.TotalMilliseconds)
                    .connectionTimeoutMs(retryStrategy.ToCuratorConnectionTimeout())
                    .retryPolicy(retryStrategy.ToCuratorRetryPolicy())
                    .@namespace(string.IsNullOrWhiteSpace(nameSpace) ? null : nameSpace.TrimStart('/'))
                    .canBeReadOnly(canBeReadonly)
                    .build(),
                log,
                new ConnectionStringRandomizer(ensembleProvider))
        {
        }

        private ZooKeeperClient(CuratorFramework curator, ILog log, ConnectionStringRandomizer connectionStringRandomizer)
        {
            this.connectionStringRandomizer = connectionStringRandomizer;
            Curator = curator;
            Curator.getConnectionStateListenable()
                .addListener(
                    new ConnectionListener(
                        state =>
                        {
                            RandomizeConnectionStringIfNeeded(state);

                            if (ConnectionStateChanged != null)
                                ConnectionStateChanged(state);
                        })
                );

            baseLog = log;
            Log = log.ForContext("ZK");
            InterceptLogger();

            watcherWrappers = new ConcurrentDictionary<IWatcher, WatcherWrapper>(new ReferenceEqualityComparer<IWatcher>());
        }

        public bool IsConnected => Curator.getZookeeperClient().isConnected();

        public bool IsStarted => Curator.getState() == CuratorFrameworkState.STARTED;

        public long SessionId => Curator.getZookeeperClient().getZooKeeper().getSessionId();

        public byte[] SessionPassword => Curator.getZookeeperClient().getZooKeeper().getSessionPasswd();

        public TimeSpan SessionTimeout => Curator.getZookeeperClient().getZooKeeper().getSessionTimeout().Milliseconds();

        public void Start()
        {
            if (Curator.getState().Equals(CuratorFrameworkState.LATENT))
                try
                {
                    Curator.start();
                }
                catch (IllegalStateException)
                {
                }
        }

        public void Dispose()
        {
            if (Curator.getState().Equals(CuratorFrameworkState.STOPPED))
                return;
            try
            {
                Curator.close();
            }
            catch (UnsupportedOperationException)
            {
            }

            ConnectionStateChanged = null;
        }

        public ZooKeeperResult<string> Create(string path, byte[] data, CreateMode createMode, bool withProtection = false)
        {
            LogCreate(path, data, createMode, withProtection);
            if (data != null && data.Length > maxDataLength)
            {
                LogUnreasonableLength(data.Length);
                return new ZooKeeperResult<string>(ZooKeeperStatus.BadArguments, path);
            }

            return ExecuteOperation(
                path,
                () =>
                {
                    var builder = Curator.create();
                    builder = (CreateBuilder) builder.withMode(org.apache.zookeeper.CreateMode.fromFlag((int) createMode));
                    // (iloktionov): Для sequential-нод может понадобиться защита от false-negative в виде GUID'а в имени ноды.
                    if (withProtection && (createMode == CreateMode.PersistentSequential || createMode == CreateMode.EphemeralSequential))
                        builder = (CreateBuilder) builder.withProtection();
                    return (string) builder
                        .creatingParentsIfNeeded()
                        .forPath(path, data ?? new byte[0]);
                });
        }

        public ZooKeeperResult Delete(string path, int version = -1, bool deleteChildrenIfNeeded = false)
        {
            LogDelete(path, version, deleteChildrenIfNeeded);
            return ExecuteOperation(
                path,
                () =>
                {
                    var builder = Curator.delete();
                    builder = (DeleteBuilder) builder.withVersion(version);
                    if (deleteChildrenIfNeeded)
                        builder = (DeleteBuilder) builder.deletingChildrenIfNeeded();
                    builder.forPath(path);
                });
        }

        public ZooKeeperResult SetData(string path, byte[] data, int version = -1)
        {
            LogSetData(path, data, version);
            if (data != null && data.Length > maxDataLength)
            {
                LogUnreasonableLength(data.Length);
                return new ZooKeeperResult(ZooKeeperStatus.BadArguments, path);
            }

            return ExecuteOperation(
                path,
                () =>
                {
                    var builder = (SetDataBuilder) Curator.setData().withVersion(version);
                    builder.forPath(path, data ?? new byte[0]);
                });
        }

        public ZooKeeperResult<string[]> GetChildren(string path, IWatcher watcher = null)
        {
            LogGetChildren(path, watcher);
            return ExecuteOperation(
                path,
                () =>
                {
                    var builder = Curator.getChildren();
                    if (watcher != null)
                        builder = (GetChildrenBuilder) builder.usingWatcher(WrapWatcher(watcher));
                    return ((List) builder.forPath(path)).toArray().Select(o => o.ToString()).ToArray();
                });
        }

        public ZooKeeperResult<Tuple<string[], Stat>> GetChildrenWithStat(string path, IWatcher watcher = null)
        {
            LogGetChildren(path, watcher);
            return ExecuteOperation(
                path,
                () =>
                {
                    var builder = Curator.getChildren();

                    if (watcher != null)
                        builder = (GetChildrenBuilder) builder.usingWatcher(WrapWatcher(watcher));

                    var stat = new org.apache.zookeeper.data.Stat();

                    var pathable = (WatchPathable) builder.storingStatIn(stat);

                    var children = ((List) pathable.forPath(path)).toArray().Select(o => o.ToString()).ToArray();

                    return Tuple.Create(children, new Stat(stat));
                });
        }

        public ZooKeeperResult<Stat> Exists(string path, IWatcher watcher = null)
        {
            LogExists(path, watcher);
            return ExecuteOperation(
                path,
                () =>
                {
                    var builder = Curator.checkExists();
                    if (watcher != null)
                        builder = (ExistsBuilder) builder.usingWatcher(WrapWatcher(watcher));
                    var zkStat = (org.apache.zookeeper.data.Stat) builder.forPath(path);
                    return zkStat == null
                        ? null
                        : new Stat(zkStat);
                });
        }

        public ZooKeeperResult<Tuple<byte[], Stat>> GetData(string path, IWatcher watcher = null)
        {
            LogGetData(path, watcher);
            return ExecuteOperation(
                path,
                () =>
                {
                    var builder = Curator.getData();
                    var stat = new org.apache.zookeeper.data.Stat();
                    if (watcher != null)
                        builder = (GetDataBuilder) builder.usingWatcher(WrapWatcher(watcher));
                    var pathable = (WatchPathable) builder.storingStatIn(stat);
                    return new Tuple<byte[], Stat>((byte[]) pathable.forPath(path), new Stat(stat));
                });
        }

        public void KillSession(TimeSpan timeout)
        {
            if (!IsConnected)
                return;
            var connectionString = Curator.getZookeeperClient().getCurrentConnectionString();
            var zooKeeper = new ZooKeeper(connectionString, 5000, null, SessionId, SessionPassword);
            try
            {
                var watch = Stopwatch.StartNew();
                while (watch.Elapsed < timeout)
                {
                    if (zooKeeper.getState().Equals(ZooKeeper.States.CONNECTED))
                        return;
                    Thread.Sleep(100);
                }

                throw new TimeoutException($"Expected to kill session within {timeout}, but failed to do so.");
            }
            finally
            {
                zooKeeper.close();
            }
        }

        public void InterceptLogger()
        {
            LoggingAdapter.Setup(Log);
        }

        public ZooKeeperClient UsingNamespace(string nameSpace)
        {
            return new ZooKeeperClient(Curator.usingNamespace(string.IsNullOrWhiteSpace(nameSpace) ? null : nameSpace.TrimStart('/')), baseLog, connectionStringRandomizer);
        }

        internal CuratorFramework Curator { get; }

        internal ILog Log { get; }

        internal void WaitUntilConnected()
        {
            while (!Curator.getZookeeperClient().blockUntilConnectedOrTimedOut())
            {
            }
        }

        private static EnsembleProvider CreateEnsembleProvider(string connectionString, ILog log)
        {
            connectionString = connectionString.Trim();
            return new FixedEnsembleProvider(connectionString);
        }

        private ZooKeeperResult ExecuteOperation(string path, Action operation)
        {
            try
            {
                operation();
                return new ZooKeeperResult(ZooKeeperStatus.Ok, path);
            }
            catch (KeeperException error)
            {
                LogKeeperException(error);
                return new ZooKeeperResult((ZooKeeperStatus) error.code().intValue(), path);
            }
            catch (IllegalArgumentException error)
            {
                LogBadArguments(error);
                return new ZooKeeperResult(ZooKeeperStatus.BadArguments, path);
            }
            catch (IllegalStateException error)
            {
                LogIllegalClientState(error);
                return new ZooKeeperResult(ZooKeeperStatus.ClientNotRunning, path);
            }
            catch (Exception error)
            {
                LogUnexpectedException(error);
                return new ZooKeeperResult(ZooKeeperStatus.UnclassifiedError, path);
            }
        }

        private ZooKeeperResult<TPayload> ExecuteOperation<TPayload>(string path, Func<TPayload> operation)
        {
            try
            {
                return new ZooKeeperResult<TPayload>(ZooKeeperStatus.Ok, path, operation());
            }
            catch (KeeperException error)
            {
                LogKeeperException(error);
                return new ZooKeeperResult<TPayload>((ZooKeeperStatus) error.code().intValue(), path);
            }
            catch (IllegalArgumentException error)
            {
                LogBadArguments(error);
                return new ZooKeeperResult<TPayload>(ZooKeeperStatus.BadArguments, path);
            }
            catch (IllegalStateException error)
            {
                LogIllegalClientState(error);
                return new ZooKeeperResult<TPayload>(ZooKeeperStatus.ClientNotRunning, path);
            }
            catch (Exception error)
            {
                LogUnexpectedException(error);
                return new ZooKeeperResult<TPayload>(ZooKeeperStatus.UnclassifiedError, path);
            }
        }

        private WatcherWrapper WrapWatcher(IWatcher watcher)
        {
            return watcherWrappers.GetOrAdd(watcher, w => new WatcherWrapper(w));
        }

        private void RandomizeConnectionStringIfNeeded(ConnectionState newConnectionState)
        {
            if (newConnectionState == ConnectionState.Suspended && connectionStringRandomizer.RandomizeIfNeeded())
            {
                Log.Info("Randomized the order of replicas in connection string in response to disconnection event.");
            }
        }

        #region Logging

        private void LogKeeperException(KeeperException error)
        {
            Log.Warn("Operation failed: " + error.getMessage());
        }

        private void LogBadArguments(IllegalArgumentException error)
        {
            Log.Error("One or more argument(s) was invalid: " + error.getMessage());
        }

        private void LogIllegalClientState(IllegalStateException error)
        {
            Log.Error("Operation is not allowed due to client state: {0}", error);
        }

        private void LogUnexpectedException(Exception error)
        {
            Log.Error("Unexpected error: {0}", error);
        }

        private void LogUnreasonableLength(int length)
        {
            Log.Error("Supplied data has unreasonably high length = {0}.", length);
        }

        private void LogCreate(string path, byte[] data, CreateMode createMode, bool withProtection)
        {
            Log.Debug(
                "Trying to create node '{0}' with mode '{1}' and data length {2}{3}.",
                path,
                createMode,
                data == null ? 0 : data.Length,
                withProtection ? " with protection" : "");
        }

        private void LogDelete(string path, int version, bool deleteChildrenIfNeeded)
        {
            Log.Debug("Trying to delete node '{0}' with version {1}. {2}", path, version, deleteChildrenIfNeeded ? "Will delete children if needed." : "");
        }

        private void LogSetData(string path, byte[] data, int version)
        {
            Log.Debug("Trying to set data for node '{0}' with version {1}. Data length = {2}.", path, version, data == null ? 0 : data.Length);
        }

        private void LogGetChildren(string path, IWatcher watcher)
        {
            Log.Debug("Trying to get children for node '{0}'. Watching = {1}.", path, watcher != null);
        }

        private void LogExists(string path, IWatcher watcher)
        {
            Log.Debug("Trying to check existance for node '{0}'. Watching = {1}.", path, watcher != null);
        }

        private void LogGetData(string path, IWatcher watcher)
        {
            Log.Debug("Trying to get data for node '{0}'. Watching = {1}.", path, watcher != null);
        }

        #endregion
    }
}