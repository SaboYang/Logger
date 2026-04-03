using System.Collections.ObjectModel;
using Logger.Core.Models;

namespace Logger.Core
{
    public interface ILogViewSource
    {
        object SyncRoot { get; }

        ObservableCollection<LogEntry> Entries { get; }

        int MaxEntries { get; set; }
    }
}
