using System;
using org.apache.curator;
using org.apache.curator.retry;

namespace Vostok.Zookeeper.Client
{
	/// <summary>
	/// Представляет стратегию повторения ZK-операций в случае проблем с соединением.
	/// </summary>
	public class RetryStrategy
	{
		/// <param name="maxAttempts">Максимальное количество попыток.</param>
		/// <param name="attemptTimeout">Максимальное время на одну попытку.</param>
		/// <param name="attemptDelay">Задержка между попытками.</param>
		public RetryStrategy(int maxAttempts, TimeSpan attemptTimeout, TimeSpan attemptDelay)
		{
			MaxAttempts = maxAttempts;
			AttemptTimeout = attemptTimeout;
			AttemptDelay = attemptDelay;
		}

		/// <summary>
		/// Создает стратегию по умолчанию (3 попытки, таймаут каждой равен таймауту сессии).
		/// </summary>
		/// <param name="sessionTimeout">Таймаут сессии.</param>
		public static RetryStrategy CreateDefault(TimeSpan sessionTimeout)
		{
			return new RetryStrategy(3, sessionTimeout, TimeSpan.FromSeconds(5));
		}

		/// <summary>
		/// Создает стратегию бесконечных попыток (попытки не ограничены, таймаут каждой равен таймауту сессии).
		/// </summary>
		/// <param name="sessionTimeout">Таймаут сессии.</param>
		public static RetryStrategy CreateInfinite(TimeSpan sessionTimeout)
		{
			return new RetryStrategy(int.MaxValue, sessionTimeout, TimeSpan.FromSeconds(5));
		}

		/// <summary>
		/// Максимальное количество попыток.
		/// </summary>
		public int MaxAttempts { get; private set; }
		
		/// <summary>
		/// Максимальное время на одну попытку.
		/// </summary>
		public TimeSpan AttemptTimeout { get; private set; }
		
		/// <summary>
		/// Задержка между попытками.
		/// </summary>
		public TimeSpan AttemptDelay { get; private set; }

		internal RetryPolicy ToCuratorRetryPolicy()
		{
			return new RetryNTimes(MaxAttempts, (int) AttemptDelay.TotalMilliseconds);
		}

		internal int ToCuratorConnectionTimeout()
		{
			return (int) AttemptTimeout.TotalMilliseconds;
		}

		#region Equality members
		protected bool Equals(RetryStrategy other)
		{
			return MaxAttempts == other.MaxAttempts
				&& AttemptTimeout.Equals(other.AttemptTimeout)
				&& AttemptDelay.Equals(other.AttemptDelay);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj)) return false;
			if (ReferenceEquals(this, obj)) return true;
			if (obj.GetType() != GetType()) return false;
			return Equals((RetryStrategy)obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				var hashCode = MaxAttempts;
				hashCode = (hashCode * 397) ^ AttemptTimeout.GetHashCode();
				hashCode = (hashCode * 397) ^ AttemptDelay.GetHashCode();
				return hashCode;
			}
		} 
		#endregion
	}
}