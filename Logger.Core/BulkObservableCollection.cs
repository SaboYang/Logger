using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Logger.Core
{
    internal class BulkObservableCollection<T> : ObservableCollection<T>
    {
        public void AddRange(IList<T> items)
        {
            if (items == null || items.Count == 0)
            {
                return;
            }

            CheckReentrancy();

            int startIndex = Count;
            for (int index = 0; index < items.Count; index++)
            {
                Items.Add(items[index]);
            }

            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, (System.Collections.IList)items, startIndex));
        }

        public void RemoveRange(int index, int count)
        {
            if (count <= 0)
            {
                return;
            }

            CheckReentrancy();

            List<T> removedItems = new List<T>(count);
            for (int current = 0; current < count; current++)
            {
                removedItems.Add(Items[index]);
                Items.RemoveAt(index);
            }

            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItems, index));
        }

        public void ReplaceAll(IList<T> items)
        {
            CheckReentrancy();

            Items.Clear();

            if (items != null)
            {
                for (int index = 0; index < items.Count; index++)
                {
                    Items.Add(items[index]);
                }
            }

            OnPropertyChanged(new PropertyChangedEventArgs(nameof(Count)));
            OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
