using ClientServerApp.Desktop.NavigationServices;

namespace ClientServerApp.Desktop.ViewModels
{
	public class MainWindowViewModel : ViewModelBase
	{
		private readonly NavigationStore _navigationStore;
		public ViewModelBase CurrentViewModel => _navigationStore.CurrentViewModel;

		public MainWindowViewModel(NavigationStore navigationStore)
		{
			_navigationStore = navigationStore;
			_navigationStore.CurrentViewModelChanged += OnCurrentViewModelChanged;
		}
		private void OnCurrentViewModelChanged()
		{
			OnPropertyChanged(nameof(CurrentViewModel));
		}
	}
}
