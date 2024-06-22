using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;

namespace AwqatSalaat.Helpers
{
    // using SynchronizationContext is necessary here when this class is used with Windows App SDK
    // because WinUI 3 still doesn't support dispatching PropertyChanged event to UI thread
    public class ObservableObject : INotifyPropertyChanged
    {
        private class PropertyChangedEventState
        {
            public readonly ObservableObject Sender;
            public readonly PropertyChangedEventArgs Args;

            public PropertyChangedEventState(ObservableObject sender, PropertyChangedEventArgs args)
            {
                Sender = sender;
                Args = args;
            }
        }

        private event PropertyChangedEventHandler propertyChanged;
        private SynchronizationContext synchronizationContext;

        public event PropertyChangedEventHandler PropertyChanged
        {
            add
            {
                
                if (synchronizationContext is null && !(SynchronizationContext.Current is null))
                {
                    if (SynchronizationContext.Current.GetType().FullName.StartsWith("Microsoft.UI"))
                    {
                        synchronizationContext = SynchronizationContext.Current;
                    }
                }

                propertyChanged += value;
            }
            remove
            {
                propertyChanged -= value;
            }
        }

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var args = new PropertyChangedEventArgs(propertyName);

            if (synchronizationContext is null)
            {
                propertyChanged?.Invoke(this, args);
            }
            else
            {
                synchronizationContext.Post((state) =>
                {
                    PropertyChangedEventState eventState = (PropertyChangedEventState)state;
                    eventState.Sender.propertyChanged?.Invoke(eventState.Sender, eventState.Args);
                },
                new PropertyChangedEventState(this, args));
            }
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (!Equals(field, value))
            {
                field = value;
                OnPropertyChanged(propertyName);
                return true;
            }

            return false;
        }
    }
}
