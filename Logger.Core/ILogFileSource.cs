namespace Logger.Core
{
    public interface ILogFileSource
    {
        bool IsFileOutputEnabled { get; }

        string LogFilePath { get; }
    }
}
