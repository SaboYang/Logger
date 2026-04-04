using System;
using System.Collections.Generic;
using System.Threading;
using Logger.Core;
using Logger.Core.Models;

namespace Logger.WinForms.Demo
{
    internal sealed class SlowTextFileLogStorageBackendFactory : ILogStorageBackendFactory
    {
        private readonly TextFileLogStorageBackendFactory _innerFactory;
        private readonly int _delayMilliseconds;

        public SlowTextFileLogStorageBackendFactory(
            string logRootDirectoryPath,
            int delayMilliseconds,
            LogFileRollingMode rollingMode = LogFileRollingMode.Day)
        {
            _innerFactory = new TextFileLogStorageBackendFactory(logRootDirectoryPath, rollingMode);
            _delayMilliseconds = Math.Max(0, delayMilliseconds);
        }

        public ILogStorageBackend CreateBackend(LogStorageContext context)
        {
            return new SlowTextFileLogStorageBackend(
                _innerFactory.CreateBackend(context),
                _delayMilliseconds);
        }

        private sealed class SlowTextFileLogStorageBackend : ILogStorageBackend, ILogFileSource
        {
            private readonly ILogStorageBackend _innerBackend;
            private readonly ILogFileSource _fileSource;
            private readonly int _delayMilliseconds;

            public SlowTextFileLogStorageBackend(ILogStorageBackend innerBackend, int delayMilliseconds)
            {
                _innerBackend = innerBackend ?? throw new ArgumentNullException(nameof(innerBackend));
                _fileSource = innerBackend as ILogFileSource;
                _delayMilliseconds = delayMilliseconds;
            }

            public bool IsFileOutputEnabled
            {
                get { return _fileSource != null && _fileSource.IsFileOutputEnabled; }
            }

            public string LogFilePath
            {
                get { return _fileSource != null ? _fileSource.LogFilePath : null; }
            }

            public void WriteBatch(IReadOnlyList<LogEntry> entries, LogStorageContext context)
            {
                if (_delayMilliseconds > 0)
                {
                    Thread.Sleep(_delayMilliseconds);
                }

                _innerBackend.WriteBatch(entries, context);
            }
        }
    }
}
