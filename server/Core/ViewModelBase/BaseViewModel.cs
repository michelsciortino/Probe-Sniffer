using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Core.ViewModelBase
{
    /// <summary>
	/// A base view model that fires Property Changed events as needed
	/// </summary>
	public class BaseViewModel : INotifyPropertyChanged
    {
        /// <summary>
        /// The event that is fired when any child property changes its value
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged = (sender, e) => { };

        /// <summary>
        ///Call this to fire a<see cref="PropertyChanged"/> event
        /// </summary>
        /// <param name="propertyName"></param>
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
