﻿using System;

namespace Vostok.Zookeeper.Client
{
	public interface IZooKeeperClient : IDisposable
	{
		/// <summary>
		/// Запускает экземпляр <see cref="IZooKeeperClient"/>, инициируя асинхронный процесс установления соединения.
		/// </summary>
		void Start();

		/// <summary>
		/// <para>Создает ноду по указанному пути. Содержимое ноды будет равно переданному массиву байтов.</para>
		/// <para>В случае отсутствия необходимых родительских нод создает их с пустым содержимым.</para>
		/// <para>В случае успеха вызывает срабатывание обработчиков, установленных с помощью <see cref="Exists"/> и <see cref="GetChildren"/></para>
		/// <para>В случае, если нода уже существует, возвращает статус <see cref="ZooKeeperStatus.NodeExists"/>.</para>
		/// <para>В случае, если родительская нода является эфемерной, возаращает статус <see cref="ZooKeeperStatus.NoChildrenForEphemerals"/>.</para>
		/// </summary>
		/// <param name="path">Полный путь до ноды (например, "/foo/bar").</param>
		/// <param name="data">Новое содержимое ноды. Не может быть длиннее 1 МБ.</param>
		/// <param name="createMode">Тип ноды.</param>
		/// <param name="withProtection">В случае создания sequential-ноды добавляет к имени GUID-префикс, позволяющий распознать собственную ноду после false negative (когда сервер все-таки ее создал, а клиент не дождался).</param>
		/// <returns>Результат - результирующий полный путь созданной ноды (может отличаться от переданного при флаге sequential).</returns>
		ZooKeeperResult<string> Create(string path, byte[] data, CreateMode createMode, bool withProtection = false);

		/// <summary>
		/// <para>Удаляет ноду по указанному пути, если ее версия совпадает с указанной.</para>
		/// <para>В случае успеха вызывает срабатывание обработчиков, установленных с помощью <see cref="Exists"/> и <see cref="GetChildren"/></para>
		/// <para>В случае, если нода не существует, возвращает статус <see cref="ZooKeeperStatus.NoNode"/>.</para>
		/// <para>В случае, если нода имеет дочерние и не указан параметр <see cref="deleteChildrenIfNeeded"/>, возвращает статус <see cref="ZooKeeperStatus.NotEmpty"/>.</para>
		/// <para>В случае, если указанная версия не совпадает с актуальной, возвращает статус <see cref="ZooKeeperStatus.BadVersion"/>.</para>
		/// </summary>
		/// <param name="path">Полный путь до ноды (например, "/foo/bar").</param>
		/// <param name="version">Ожидаемая версия ноды (-1 соответствует любой версии).</param>
		/// <param name="deleteChildrenIfNeeded">Определяет, удалять ли автоматически дочерние ноды в случае обнаружения таковых.</param>
		/// <returns></returns>
		ZooKeeperResult Delete(string path, int version = -1, bool deleteChildrenIfNeeded = false);
		
		/// <summary>
		/// <para>Устанавливает содержимое ноды по указанному пути, если ее версия совпадает с указанной. Размер содержимого не должен превышать 1 МБ!</para>
		/// <para>В случае успеха вызывает срабатывание обработчиков, установленных с помощью <see cref="GetData"/></para>
		/// <para>В случае, если нода не существует, возвращает статус <see cref="ZooKeeperStatus.NoNode"/>.</para>
		/// <para>В случае, если указанная версия не совпадает с актуальной, возвращает статус <see cref="ZooKeeperStatus.BadVersion"/>.</para>
		/// </summary>
		/// <param name="path">Полный путь до ноды (например, "/foo/bar").</param>
		/// <param name="data">Новое содержимое ноды. Не может быть длиннее 1 МБ.</param>
		/// <param name="version">Ожидаемая версия ноды (-1 соответствует любой версии).</param>
		/// <returns></returns>
		ZooKeeperResult SetData(string path, byte[] data, int version = -1);
		
		/// <summary>
		/// <para>Проверяет существование ноды по указанному пути.</para>
		/// <para>Если передан отличный от null <see cref="IWatcher"/>, он будет вызван в случае создания/удаления/изменения ноды по указанному пути.</para>
		/// <para>ВАЖНО: обработчки событий срабатывают ровно один раз, после чего удаляются сервером.</para>
		/// </summary>
		/// <param name="path">Полный путь до ноды (например, "/foo/bar").</param>
		/// <param name="watcher">Обработчик события изменения ноды.</param>
		/// <returns>Результат - статистика ноды, если она существует, и null в противном случае.</returns>
		ZooKeeperResult<Stat> Exists(string path, IWatcher watcher = null);
		
		/// <summary>
		/// <para>Возвращает список дочерних нод.</para>
		/// <para>Если передан отличный от null <see cref="IWatcher"/>, он будет вызван в случае удаления ноды по указанному пути или создания дочерних нод.</para>
		/// <para>ВАЖНО: обработчки событий срабатывают ровно один раз, после чего удаляются сервером.</para>
		/// <para>В случае, если нода не существует, возвращает статус <see cref="ZooKeeperStatus.NoNode"/>.</para>
		/// </summary>
		/// <param name="path">Полный путь до ноды (например, "/foo/bar").</param>
		/// <param name="watcher">Обработчик события изменения ноды.</param>
		/// <returns>Результат - неупорядоченный массив имен (не полных путей!) дочерних нод.</returns>
		ZooKeeperResult<string[]> GetChildren(string path, IWatcher watcher = null);

        /// <summary>
        /// <para>Возвращает список дочерних нод и статистику ноды по указанному пути..</para>
        /// <para>Если передан отличный от null <see cref="IWatcher"/>, он будет вызван в случае удаления ноды по указанному пути или создания дочерних нод.</para>
        /// <para>ВАЖНО: обработчики событий срабатывают ровно один раз, после чего удаляются сервером.</para>
        /// <para>В случае, если нода не существует, возвращает статус <see cref="ZooKeeperStatus.NoNode"/>.</para>
        /// </summary>
        /// <param name="path">Полный путь до ноды (например, "/foo/bar").</param>
        /// <param name="watcher">Обработчик события изменения ноды.</param>
        /// <returns>Результат - неупорядоченный массив имен (не полных путей!) дочерних нод и статистика parent-ноды.</returns>
        ZooKeeperResult<Tuple<string[], Stat>> GetChildrenWithStat(string path, IWatcher watcher = null);

        /// <summary>
        /// <para>Возвращает содержимое и статистику ноды по указанному пути.</para>
        /// <para>Если передан отличный от null <see cref="IWatcher"/>, он будет вызван в случае изменения данных ноды по указанному пути.</para>
        /// <para>ВАЖНО: обработчки событий срабатывают ровно один раз, после чего удаляются сервером.</para>
        /// <para>В случае, если нода не существует, возвращает статус <see cref="ZooKeeperStatus.NoNode"/>.</para>
        /// </summary>
        /// <param name="path">Полный путь до ноды (например, "/foo/bar").</param>
        /// <param name="watcher">Обработчик события изменения ноды.</param>
        /// <returns>Результат - blob с содержимым ноды и ее статистика.</returns>
        ZooKeeperResult<Tuple<byte[], Stat>> GetData(string path, IWatcher watcher = null);

		/// <summary>
		/// Событие, выстреливающее при любом изменении состоянии клиентского соединения.
		/// Никогда не вызывается конкурентно.
		/// </summary>
		event Action<ConnectionState> ConnectionStateChanged; 

        /// <summary>
        /// Возвращает true, если в текущий момент соединение с кластером установлено, или false в противном случае.
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// Возвращает true, если в текущий момент запущен экземпляр <see cref="IZooKeeperClient"/>, или false в противном случае.
        /// </summary>
        bool IsStarted { get; }

        /// <summary>
        /// Возвращает идентификатор сессии, если она уже установлена, или 0 в противном случае.
        /// </summary>
        long SessionId { get; }

        /// <summary>
        /// Возвращает таймаут сессии, если она уже установлена, или 0 в противном случае.
        /// </summary>
        TimeSpan SessionTimeout { get; }
    }
}