using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml.Linq;
using Logger.Core.Models;

namespace Logger.Core
{
    internal static class LoggerConfiguration
    {
        private const string ConfigurationFileName = "Logger.config";
        private const string ConfigurationRootElementName = "loggerConfiguration";
        private const string DefaultProfileElementName = "default";
        private const string LoggerProfileElementName = "logger";
        private const string NameAttributeName = "name";
        private const string LogRootDirectoryPathAttributeName = "logRootDirectoryPath";
        private const string RollingModeAttributeName = "rollingMode";
        private const string RollingRetentionDaysAttributeName = "rollingRetentionDays";

        public static ILoggerFactory CreateDefaultFactory()
        {
            return new ConfigurableLogStoreLoggerFactory();
        }

        public static LoggerRuntimeSettings ResolveRuntimeSettings(string loggerName)
        {
            LoggerConfigurationSnapshot snapshot = GetSnapshot();
            LoggerRuntimeSettings defaultSettings = LoggerRuntimeSettings.CreateDefault();

            if (snapshot == null)
            {
                return defaultSettings;
            }

            LoggerRuntimeSettings resolvedSettings = LoggerRuntimeSettings.Merge(defaultSettings, snapshot.DefaultSettings);

            LoggerRuntimeSettings namedSettings;
            if (snapshot.NamedSettings.TryGetValue(LoggerPathUtility.NormalizeLoggerName(loggerName), out namedSettings))
            {
                resolvedSettings = LoggerRuntimeSettings.Merge(resolvedSettings, namedSettings);
            }

            return resolvedSettings;
        }

        private static LoggerConfigurationSnapshot GetSnapshot()
        {
            string configurationFilePath = GetConfigurationFilePath();
            if (string.IsNullOrWhiteSpace(configurationFilePath) || !File.Exists(configurationFilePath))
            {
                return null;
            }

            DateTime lastWriteTimeUtc = File.GetLastWriteTimeUtc(configurationFilePath);
            lock (SyncRoot)
            {
                if (_snapshot != null &&
                    string.Equals(_snapshot.ConfigurationFilePath, configurationFilePath, StringComparison.OrdinalIgnoreCase) &&
                    _snapshot.LastWriteTimeUtc == lastWriteTimeUtc)
                {
                    return _snapshot;
                }

                _snapshot = LoadSnapshot(configurationFilePath, lastWriteTimeUtc);
                return _snapshot;
            }
        }

        private static LoggerConfigurationSnapshot LoadSnapshot(string filePath, DateTime lastWriteTimeUtc)
        {
            try
            {
                XDocument document = XDocument.Load(filePath);
                XElement root = document.Root;
                if (root == null)
                {
                    return EmptySnapshot(filePath, lastWriteTimeUtc);
                }

                XElement configurationRoot = ResolveConfigurationRoot(root);
                if (configurationRoot == null)
                {
                    return EmptySnapshot(filePath, lastWriteTimeUtc);
                }

                string baseDirectory = Path.GetDirectoryName(filePath);
                if (string.IsNullOrWhiteSpace(baseDirectory))
                {
                    baseDirectory = AppContext.BaseDirectory;
                }

                LoggerRuntimeSettings defaultSettings = null;
                Dictionary<string, LoggerRuntimeSettings> namedSettings =
                    new Dictionary<string, LoggerRuntimeSettings>(StringComparer.OrdinalIgnoreCase);

                foreach (XElement child in configurationRoot.Elements())
                {
                    if (string.Equals(child.Name.LocalName, DefaultProfileElementName, StringComparison.OrdinalIgnoreCase))
                    {
                        defaultSettings = ParseRuntimeSettings(child, baseDirectory);
                        continue;
                    }

                    if (!string.Equals(child.Name.LocalName, LoggerProfileElementName, StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string profileName = GetAttributeValue(child, NameAttributeName);
                    if (string.IsNullOrWhiteSpace(profileName))
                    {
                        continue;
                    }

                    namedSettings[LoggerPathUtility.NormalizeLoggerName(profileName)] = ParseRuntimeSettings(child, baseDirectory);
                }

                return new LoggerConfigurationSnapshot(filePath, lastWriteTimeUtc, defaultSettings, namedSettings);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
            catch (System.Xml.XmlException)
            {
            }

            return EmptySnapshot(filePath, lastWriteTimeUtc);
        }

        private static LoggerConfigurationSnapshot EmptySnapshot(string filePath, DateTime lastWriteTimeUtc)
        {
            return new LoggerConfigurationSnapshot(
                filePath,
                lastWriteTimeUtc,
                null,
                new Dictionary<string, LoggerRuntimeSettings>(StringComparer.OrdinalIgnoreCase));
        }

        private static XElement ResolveConfigurationRoot(XElement root)
        {
            if (root == null)
            {
                return null;
            }

            if (string.Equals(root.Name.LocalName, ConfigurationRootElementName, StringComparison.OrdinalIgnoreCase))
            {
                return root;
            }

            foreach (XElement child in root.Elements())
            {
                if (string.Equals(child.Name.LocalName, ConfigurationRootElementName, StringComparison.OrdinalIgnoreCase))
                {
                    return child;
                }
            }

            return root;
        }

        private static LoggerRuntimeSettings ParseRuntimeSettings(XElement element, string baseDirectory)
        {
            LoggerRuntimeSettings settings = LoggerRuntimeSettings.CreateEmpty();
            if (element == null)
            {
                return settings;
            }

            settings.LogRootDirectoryPath = ResolveRelativePath(
                GetAttributeValue(element, LogRootDirectoryPathAttributeName),
                baseDirectory);

            string rollingModeText = GetAttributeValue(element, RollingModeAttributeName);
            LogFileRollingMode rollingMode;
            if (Enum.TryParse(rollingModeText, true, out rollingMode))
            {
                settings.RollingMode = rollingMode;
            }

            string retentionDaysText = GetAttributeValue(element, RollingRetentionDaysAttributeName);
            int retentionDays;
            if (int.TryParse(retentionDaysText, NumberStyles.Integer, CultureInfo.InvariantCulture, out retentionDays))
            {
                settings.RollingRetentionDays = retentionDays;
            }

            return settings;
        }

        private static string ResolveRelativePath(string path, string baseDirectory)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            string trimmedPath = path.Trim();
            if (Path.IsPathRooted(trimmedPath))
            {
                return trimmedPath;
            }

            if (string.IsNullOrWhiteSpace(baseDirectory))
            {
                return trimmedPath;
            }

            return Path.Combine(baseDirectory, trimmedPath);
        }

        private static string GetConfigurationFilePath()
        {
            return Path.Combine(AppContext.BaseDirectory, ConfigurationFileName);
        }

        private static string GetAttributeValue(XElement element, string attributeName)
        {
            if (element == null || string.IsNullOrWhiteSpace(attributeName))
            {
                return null;
            }

            XAttribute attribute = element.Attribute(attributeName);
            if (attribute == null || string.IsNullOrWhiteSpace(attribute.Value))
            {
                return null;
            }

            return attribute.Value.Trim();
        }

        private static readonly object SyncRoot = new object();
        private static LoggerConfigurationSnapshot _snapshot;

        internal sealed class LoggerRuntimeSettings
        {
            public static LoggerRuntimeSettings CreateDefault()
            {
                return new LoggerRuntimeSettings
                {
                    LogRootDirectoryPath = null,
                    RollingMode = LogFileRollingMode.Day,
                    RollingRetentionDays = 30
                };
            }

            public static LoggerRuntimeSettings CreateEmpty()
            {
                return new LoggerRuntimeSettings();
            }

            public static LoggerRuntimeSettings Merge(LoggerRuntimeSettings fallback, LoggerRuntimeSettings overrideSettings)
            {
                LoggerRuntimeSettings result = CreateDefault();
                Apply(result, fallback);
                Apply(result, overrideSettings);
                return result;
            }

            private static void Apply(LoggerRuntimeSettings target, LoggerRuntimeSettings source)
            {
                if (target == null || source == null)
                {
                    return;
                }

                if (!string.IsNullOrWhiteSpace(source.LogRootDirectoryPath))
                {
                    target.LogRootDirectoryPath = source.LogRootDirectoryPath;
                }

                if (source.RollingMode.HasValue)
                {
                    target.RollingMode = source.RollingMode.Value;
                }

                if (source.RollingRetentionDays.HasValue && source.RollingRetentionDays.Value > 0)
                {
                    target.RollingRetentionDays = source.RollingRetentionDays.Value;
                }
            }

            public string LogRootDirectoryPath { get; set; }

            public LogFileRollingMode? RollingMode { get; set; }

            public int? RollingRetentionDays { get; set; }
        }

        private sealed class LoggerConfigurationSnapshot
        {
            public LoggerConfigurationSnapshot(
                string configurationFilePath,
                DateTime lastWriteTimeUtc,
                LoggerRuntimeSettings defaultSettings,
                Dictionary<string, LoggerRuntimeSettings> namedSettings)
            {
                ConfigurationFilePath = configurationFilePath;
                LastWriteTimeUtc = lastWriteTimeUtc;
                DefaultSettings = defaultSettings;
                NamedSettings = namedSettings ?? new Dictionary<string, LoggerRuntimeSettings>(StringComparer.OrdinalIgnoreCase);
            }

            public string ConfigurationFilePath { get; }

            public DateTime LastWriteTimeUtc { get; }

            public LoggerRuntimeSettings DefaultSettings { get; }

            public Dictionary<string, LoggerRuntimeSettings> NamedSettings { get; }
        }
    }

    internal sealed class ConfigurableLogStoreLoggerFactory : ILoggerFactory
    {
        private readonly LogLevel _minimumLevel;
        private readonly int _maxBufferedSessionEntries;
        private readonly int _maxPendingStorageEntries;
        private readonly string _spoolRootDirectoryPath;
        private readonly LogSpoolFlushMode _spoolFlushMode;

        public ConfigurableLogStoreLoggerFactory(
            LogLevel minimumLevel = LogLevel.Trace,
            int maxBufferedSessionEntries = 5000,
            int maxPendingStorageEntries = 5000,
            string spoolRootDirectoryPath = null,
            LogSpoolFlushMode spoolFlushMode = LogSpoolFlushMode.Buffered)
        {
            _minimumLevel = minimumLevel;
            _maxBufferedSessionEntries = Math.Max(1, maxBufferedSessionEntries);
            _maxPendingStorageEntries = Math.Max(1, maxPendingStorageEntries);
            _spoolRootDirectoryPath = spoolRootDirectoryPath;
            _spoolFlushMode = spoolFlushMode;
        }

        public ILoggerOutput CreateLogger(string name)
        {
            LoggerConfiguration.LoggerRuntimeSettings settings = LoggerConfiguration.ResolveRuntimeSettings(name);
            LogStoreLoggerFactory innerFactory = new LogStoreLoggerFactory(
                logRootDirectoryPath: settings.LogRootDirectoryPath,
                minimumLevel: _minimumLevel,
                rollingMode: settings.RollingMode ?? LogFileRollingMode.Day,
                rollingRetentionDays: settings.RollingRetentionDays.HasValue && settings.RollingRetentionDays.Value > 0
                    ? settings.RollingRetentionDays.Value
                    : 30,
                maxBufferedSessionEntries: _maxBufferedSessionEntries,
                maxPendingStorageEntries: _maxPendingStorageEntries,
                spoolRootDirectoryPath: _spoolRootDirectoryPath,
                spoolFlushMode: _spoolFlushMode);

            return innerFactory.CreateLogger(name);
        }
    }
}
