namespace ModelConverter.Views
{
    using System.ComponentModel;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Base binding source
    /// </summary>
    public abstract class BindingSource : INotifyPropertyChanged
    {
        /// <summary>
        /// Property changed event
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raise property changed event
        /// </summary>
        /// <param name="name">Name of the changed property</param>
        public void RaisePropertyChanged([CallerMemberName] string name = null)
        {
            if (!string.IsNullOrWhiteSpace(name) && this.PropertyChanged != null)
            {
                this.PropertyChanged.Invoke(this, new PropertyChangedEventArgs(name));
            }
        }
    }
}