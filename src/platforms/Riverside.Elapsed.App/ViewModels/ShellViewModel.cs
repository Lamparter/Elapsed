using Riverside.Elapsed.App.Services.Auth;

namespace Riverside.Elapsed.App.ViewModels;

public sealed class ShellViewModel
{
	private readonly INavigator _navigator;

	public ShellViewModel(ILapseAuthService authService, INavigator navigator)
	{
		_navigator = navigator;
		authService.LoggedOut += OnLoggedOut;
	}

	private async void OnLoggedOut(object? sender, EventArgs e)
	{
		await _navigator.NavigateViewModelAsync<LoginViewModel>(this, qualifier: Qualifiers.ClearBackStack);
	}
}
