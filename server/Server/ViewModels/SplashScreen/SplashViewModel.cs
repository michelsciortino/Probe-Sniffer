using System.Windows.Controls;

namespace Server.ViewModels.SplashScreen
{
    public class SplashViewModel: Core.ViewModelBase.BaseViewModel
    {
        #region Private

        private Page _currentPage;

        #endregion

        #region Public Properties
        public Page CurrentPage
        {
            get => _currentPage;
            set
            {
                if (value == _currentPage) return;
                _currentPage = value;
                OnPropertyChanged(nameof(CurrentPage));
            }
        }
        #endregion
    }
}
