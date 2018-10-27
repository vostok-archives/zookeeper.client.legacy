namespace Vostok.Zookeeper.Client
{
    /// <summary>
    /// Представляет интерфейс для клиентских обработчиков событий, на которые можно подписаться при операциях чтения.
    /// </summary>
    public interface IWatcher
    {
        void ProcessEvent(EventType type, string path);
    }
}