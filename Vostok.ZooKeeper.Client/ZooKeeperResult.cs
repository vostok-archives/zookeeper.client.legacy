namespace Vostok.Zookeeper.Client
{
    /// <summary>
    /// Представляет результат клиентской операции.
    /// </summary>
    public class ZooKeeperResult
    {
        public ZooKeeperResult(ZooKeeperStatus status, string path)
        {
            Status = status;
            Path = path;
        }

        /// <summary>
        /// Статус операции.
        /// </summary>
        public ZooKeeperStatus Status { get; }

        /// <summary>
        /// Путь ноды, соответствующей операции.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Возвращает true, если операция завершилась успешно.
        /// </summary>
        public bool IsSuccessful()
        {
            return Status == ZooKeeperStatus.Ok;
        }

        /// <summary>
        /// Возвращает true, если операция завершилась с системной ошибкой (проблемы с соединением, клиентские исключения).
        /// </summary>
        public bool IsSystemError()
        {
            return Status < ZooKeeperStatus.Ok && Status > ZooKeeperStatus.NoNode;
        }

        /// <summary>
        /// Возвращает true, если операция завершилась осмысленной ошибкой (предусмотренной протоколом ZK).
        /// </summary>
        public bool IsApiError()
        {
            return Status <= ZooKeeperStatus.NoNode;
        }

        /// <summary>
        /// В случае неуспешного статуса выбрасывает исключение <see cref="ZooKeeperException"/>. 
        /// </summary>
        public ZooKeeperResult EnsureSuccess()
        {
            if (!IsSuccessful())
                throw new ZooKeeperException(Status, Path);
            return this;
        }

        public override string ToString()
        {
            return string.Format("'{0}' for path '{1}'", Status, Path);
        }
    }

    /// <summary>
    /// Представляет результат клиентской операции, возвращающей значение.
    /// </summary>
    public class ZooKeeperResult<TPayload> : ZooKeeperResult
    {
        private readonly TPayload payload;

        public ZooKeeperResult(ZooKeeperStatus status, string path, TPayload payload = default)
            : base(status, path)
        {
            this.payload = payload;
        }

        /// <summary>
        /// В случае успеха возвращает результат операции. В противном случае выбрасывает исключение <see cref="ZooKeeperException"/>. 
        /// </summary>
        public TPayload Payload
        {
            get
            {
                EnsureSuccess();
                return payload;
            }
        }
    }
}