using ClientServerApp.Desktop.ViewModels;
using System;

namespace ClientServerApp.Desktop.NavigationServices
{
	public class NavigationStore
	{
		public ViewModelBase _currentViewModel;
		public ViewModelBase CurrentViewModel
		{
			get { return _currentViewModel; }
			set
			{
				_currentViewModel = value;
				OnCurrentViewModelChanged();
			}
		}

		public event Action CurrentViewModelChanged;
		private void OnCurrentViewModelChanged()
		{
			CurrentViewModelChanged?.Invoke();
		}

	}
}
