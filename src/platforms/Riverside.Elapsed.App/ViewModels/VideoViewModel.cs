using System.Collections.ObjectModel;
using Riverside.Elapsed.App.Services.Api;

namespace Riverside.Elapsed.App.ViewModels;

public partial class VideoViewModel : ObservableObject
{
	private readonly INavigator _navigator;
	private readonly IApiGlobalService _globalService;

	[ObservableProperty]
	private string _title = "Timelapse";

	[ObservableProperty]
	private string _subtitle = string.Empty;

	[ObservableProperty]
	private Uri? _playbackUrl;

	[ObservableProperty]
	private string? _activeTimelapseId;

	public VideoViewModel(INavigator navigator, IApiGlobalService globalService)
	{
		_navigator = navigator;
		_globalService = globalService;
		RelatedTimelapses = [];
		BackCommand = new AsyncRelayCommand(() => _navigator.NavigateBackAsync(this));
		OpenTimelapseCommand = new AsyncRelayCommand<MainViewModel.TimelapseCardViewModel>(OpenTimelapseAsync);
	}

	public ObservableCollection<MainViewModel.TimelapseCardViewModel> RelatedTimelapses { get; }

	public IAsyncRelayCommand BackCommand { get; }

	public IAsyncRelayCommand<MainViewModel.TimelapseCardViewModel> OpenTimelapseCommand { get; }

	public async Task InitializeAsync()
	{
		var timelapseId = ActiveTimelapseId;
		var recent = await _globalService.GetRecentTimelapsesAsync();
		if (!recent.IsSuccess || recent.Value is null)
		{
			return;
		}

		RelatedTimelapses.Clear();
		MainViewModel.TimelapseCardViewModel? selected = null;
		foreach (var timelapse in recent.Value)
		{
			var card = MainViewModel.TimelapseCardViewModel.FromModel(timelapse);
			RelatedTimelapses.Add(card);
			if (string.Equals(card.TimelapseId, timelapseId, StringComparison.Ordinal))
			{
				selected = card;
			}
		}

		selected ??= RelatedTimelapses.FirstOrDefault();
		if (selected is not null)
		{
			ApplySelectedTimelapse(selected);
		}
	}

	private Task OpenTimelapseAsync(MainViewModel.TimelapseCardViewModel? card)
	{
		if (card is not null)
		{
			ApplySelectedTimelapse(card);
		}

		return Task.CompletedTask;
	}

	private void ApplySelectedTimelapse(MainViewModel.TimelapseCardViewModel card)
	{
		ActiveTimelapseId = card.TimelapseId;
		Title = card.Title;
		Subtitle = card.OwnerText;
		PlaybackUrl = card.PlaybackUrl;
	}
}
