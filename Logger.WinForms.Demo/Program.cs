using System;
using System.Windows.Forms;

namespace Logger.WinForms.Demo
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            DemoLaunchOptions options = DemoLaunchOptions.Parse(args);
            if (options.OpenConfigDemo)
            {
                Application.Run(new LoggerConfigDemoForm());
                return;
            }

            if (options.OpenFactoryDemo)
            {
                Application.Run(new LoggerFactoryIsolatedDemoForm());
                return;
            }

            if (options.OpenFileDemo)
            {
                Application.Run(new FileLogDemoForm());
                return;
            }

            if (options.OpenStorageDemo)
            {
                Application.Run(new StorageBackendDemoForm());
                return;
            }

            if (options.OpenMemoryMetricsDemo)
            {
                Application.Run(new MemoryMetricsDemoForm());
                return;
            }

            if (options.OpenWpfHost)
            {
                using (WpfHostForm hostForm = new WpfHostForm(options.StressLogCount, options.AutoRunStressTest, options.CloseAfterStressTest))
                {
                    Application.Run(hostForm);
                    Environment.ExitCode = hostForm.AutoTestSucceeded == false ? 1 : 0;
                }

                return;
            }

            Application.Run(new MainForm());
        }

        private sealed class DemoLaunchOptions
        {
            public bool AutoRunStressTest { get; private set; }

            public bool CloseAfterStressTest { get; private set; }

            public bool OpenWpfHost { get; private set; }

            public bool OpenFactoryDemo { get; private set; }

            public bool OpenConfigDemo { get; private set; }

            public bool OpenFileDemo { get; private set; }

            public bool OpenStorageDemo { get; private set; }

            public bool OpenMemoryMetricsDemo { get; private set; }

            public int StressLogCount { get; private set; } = 30000;

            public static DemoLaunchOptions Parse(string[] args)
            {
                DemoLaunchOptions options = new DemoLaunchOptions();
                if (args == null)
                {
                    return options;
                }

                foreach (string arg in args)
                {
                    if (string.Equals(arg, "--wpf-host", StringComparison.OrdinalIgnoreCase))
                    {
                        options.OpenWpfHost = true;
                    }
                    else if (string.Equals(arg, "--factory-demo", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(arg, "--log-manager-demo", StringComparison.OrdinalIgnoreCase))
                    {
                        options.OpenFactoryDemo = true;
                    }
                    else if (string.Equals(arg, "--config-demo", StringComparison.OrdinalIgnoreCase))
                    {
                        options.OpenConfigDemo = true;
                    }
                    else if (string.Equals(arg, "--file-demo", StringComparison.OrdinalIgnoreCase))
                    {
                        options.OpenFileDemo = true;
                    }
                    else if (string.Equals(arg, "--storage-demo", StringComparison.OrdinalIgnoreCase))
                    {
                        options.OpenStorageDemo = true;
                    }
                    else if (string.Equals(arg, "--memory-demo", StringComparison.OrdinalIgnoreCase))
                    {
                        options.OpenMemoryMetricsDemo = true;
                    }
                    else if (string.Equals(arg, "--stress", StringComparison.OrdinalIgnoreCase))
                    {
                        options.OpenWpfHost = true;
                        options.AutoRunStressTest = true;
                    }
                    else if (string.Equals(arg, "--close-after-stress", StringComparison.OrdinalIgnoreCase))
                    {
                        options.CloseAfterStressTest = true;
                    }
                    else if (arg != null && arg.StartsWith("--stress-count=", StringComparison.OrdinalIgnoreCase))
                    {
                        int parsedCount;
                        if (int.TryParse(arg.Substring("--stress-count=".Length), out parsedCount) && parsedCount > 0)
                        {
                            options.StressLogCount = parsedCount;
                        }
                    }
                }

                return options;
            }
        }
    }
}
