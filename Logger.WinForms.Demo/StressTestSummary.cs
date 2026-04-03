using System;

namespace Logger.WinForms.Demo
{
    internal enum StressTestState
    {
        NotRun,
        Running,
        Succeeded,
        Failed
    }

    internal sealed class StressTestSummary
    {
        private StressTestSummary(
            string scenario,
            StressTestState state,
            int logCount,
            long durationMs,
            DateTime timestamp,
            string details)
        {
            Scenario = scenario ?? string.Empty;
            State = state;
            LogCount = logCount;
            DurationMs = durationMs;
            Timestamp = timestamp;
            Details = details ?? string.Empty;
        }

        public string Scenario { get; }

        public StressTestState State { get; }

        public int LogCount { get; }

        public long DurationMs { get; }

        public DateTime Timestamp { get; }

        public string Details { get; }

        public double Throughput
        {
            get
            {
                if (DurationMs <= 0 || LogCount <= 0)
                {
                    return 0D;
                }

                return LogCount * 1000D / DurationMs;
            }
        }

        public static StressTestSummary CreateIdle(string scenario, string details)
        {
            return new StressTestSummary(scenario, StressTestState.NotRun, 0, 0, DateTime.Now, details);
        }

        public static StressTestSummary CreateRunning(string scenario, int logCount, string details)
        {
            return new StressTestSummary(scenario, StressTestState.Running, logCount, 0, DateTime.Now, details);
        }

        public static StressTestSummary CreateSuccess(string scenario, int logCount, long durationMs, string details)
        {
            return new StressTestSummary(scenario, StressTestState.Succeeded, logCount, durationMs, DateTime.Now, details);
        }

        public static StressTestSummary CreateFailure(string scenario, int logCount, long durationMs, string details)
        {
            return new StressTestSummary(scenario, StressTestState.Failed, logCount, durationMs, DateTime.Now, details);
        }
    }
}
