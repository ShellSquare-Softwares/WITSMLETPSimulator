using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace ShellSquare.ETP.Simulator
{
    class NotifyObservableCollection<T> : ObservableCollection<T> where T : INotifyPropertyChanged
    {
        private bool m_InternalChange = false;
        public void BeginChange()
        {
            m_InternalChange = true;
        }

        public void EndChange()
        {
            m_InternalChange = false;
        }

        private void Handle(object sender, PropertyChangedEventArgs args)
        {
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset, null));
        }

        protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            if (m_InternalChange)
            {
                return;
            }

            if (e.NewItems != null)
            {
                foreach (object t in e.NewItems)
                {
                    ((T)t).PropertyChanged += Handle;
                }
            }
            if (e.OldItems != null)
            {
                foreach (object t in e.OldItems)
                {
                    ((T)t).PropertyChanged -= Handle;
                }
            }
            base.OnCollectionChanged(e);
        }
    }
}
