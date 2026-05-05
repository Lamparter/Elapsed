namespace Riverside.Elapsed.App.ViewModels;

public partial class RecordingViewModel : ObservableObject
{
	private readonly INavigator _navigator;

	[ObservableProperty]
	private string _status = "Not recording";

	[ObservableProperty]
	private bool _isPaused;

	public RecordingViewModel(INavigator navigator)
	{
		_navigator = navigator;
		PauseResumeCommand = new RelayCommand(TogglePauseResume);
		StopCommand = new RelayCommand(StopRecording);
		BackCommand = new AsyncRelayCommand(() => _navigator.NavigateBackAsync(this));
	}

	public IRelayCommand PauseResumeCommand { get; }

	public IRelayCommand StopCommand { get; }

	public IAsyncRelayCommand BackCommand { get; }

	public string PauseResumeText => IsPaused ? "Resume" : "Pause";

	partial void OnIsPausedChanged(bool value)
	{
		OnPropertyChanged(nameof(PauseResumeText));
		Status = value ? "Recording paused" : "Recording active";
	}

	private void TogglePauseResume()
	{
		IsPaused = !IsPaused;
	}

	private void StopRecording()
	{
		IsPaused = false;
		Status = "Recording stopped";
	}
}
