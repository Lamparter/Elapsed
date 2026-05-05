using Riverside.Elapsed.App.Services.Auth;

namespace Riverside.Elapsed.App.ViewModels;

public partial class LoginViewModel : ObservableObject
{
	private readonly INavigator _navigator;
	private readonly ILapseAuthService _authService;

	[ObservableProperty]
	private bool _isWorking;

	[ObservableProperty]
	private string? _message;

	public LoginViewModel(INavigator navigator, ILapseAuthService authService)
	{
		_navigator = navigator;
		_authService = authService;
		LoginCommand = new AsyncRelayCommand(LoginAsync);
	}

	public string Title => "Sign in";

	public string Description => "Authenticate with Lapse to continue.";

	public IAsyncRelayCommand LoginCommand { get; }

	private async Task LoginAsync()
	{
		IsWorking = true;
		Message = null;
		try
		{
			var result = await _authService.LoginAsync();
			if (result.IsSuccess)
			{
				await _navigator.NavigateViewModelAsync<MainViewModel>(this, qualifier: Qualifiers.ClearBackStack);
				return;
			}

			if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
			{
				Message = result.ErrorMessage;
			}
		}
		finally
		{
			IsWorking = false;
		}
	}
}
