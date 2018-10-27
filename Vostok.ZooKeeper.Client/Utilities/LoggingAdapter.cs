using System.Text;
using org.apache.log4j;
using org.apache.log4j.spi;
using org.apache.log4j.varia;
using Vostok.Logging.Abstractions;

namespace Vostok.Zookeeper.Client.Utilities
{
	/// <summary>
	/// Проксирует вывод log4j в экземпляр <see cref="ILog"/>.
	/// </summary>
	internal class LoggingAdapter : Appender
	{
		public LoggingAdapter(ILog log)
		{
			this.log = log;
		}

		public static void Setup(ILog log)
		{
			Logger.getRootLogger().removeAllAppenders();
			Logger.getRootLogger().addAppender(new LoggingAdapter(log));
		}

		public void doAppend(LoggingEvent loggingEvent)
		{
			switch (loggingEvent.getLevel().toInt())
			{
				case Priority.DEBUG_INT:
					if (log.IsEnabledForDebug())
						log.Debug(FormatLoggingEvent(loggingEvent));
					break;
				case Priority.INFO_INT:
					if (log.IsEnabledForInfo())
						log.Info(FormatLoggingEvent(loggingEvent));
					break;
				case Priority.WARN_INT:
					if (log.IsEnabledForWarn())
						log.Warn(FormatLoggingEvent(loggingEvent));
					break;
				case Priority.ERROR_INT:
					if (log.IsEnabledForError())
						log.Error(FormatLoggingEvent(loggingEvent));
					break;
				case Priority.FATAL_INT:
					if (log.IsEnabledForFatal())
						log.Fatal(FormatLoggingEvent(loggingEvent));
					break;
			}
		}

		public void addFilter(Filter f)
		{
		}

		public Filter getFilter()
		{
			var filter = new LevelRangeFilter();
			filter.setLevelMin(Level.DEBUG);
			filter.setLevelMax(Level.FATAL);
			return filter;
		}

		public void clearFilters()
		{
		}

		public void close()
		{
		}

		public string getName()
		{
			return "Log4jToKonturLoggingAdapter";
		}

		public void setErrorHandler(ErrorHandler eh)
		{
		}

		public ErrorHandler getErrorHandler()
		{
			return null;
		}

		public void setLayout(Layout l)
		{
		}

		public Layout getLayout()
		{
			return new SimpleLayout();
		}

		public void setName(string str)
		{
		}

		public bool requiresLayout()
		{
			return false;
		}

		private static string FormatLoggingEvent(LoggingEvent loggingEvent)
		{
			var exceptionInformation = loggingEvent.getThrowableStrRep();
			if (exceptionInformation == null)
				return loggingEvent.getRenderedMessage();
			var builder = new StringBuilder(loggingEvent.getRenderedMessage());
			foreach (var exceptionLine in exceptionInformation)
			{
				builder.AppendLine();
				builder.Append(exceptionLine);
			}
			return builder.ToString();
		}

		private readonly ILog log;
	}
}