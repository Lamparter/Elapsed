using System.Collections.ObjectModel;
using Riverside.Elapsed.App.Models.Global;
using Riverside.Elapsed.App.Models.Timelapses;
using Riverside.Elapsed.App.Services.Api;
using Riverside.Elapsed.App.Services.Auth;
using Riverside.Elapsed.App.Services.Build;

namespace Riverside.Elapsed.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
	private readonly INavigator _navigator;
	private readonly ILapseAuthService _authService;
	private readonly IApiGlobalService _globalService;
	private readonly BuildInfo _buildInfo;

	[ObservableProperty]
	private bool _isLoading;

	[ObservableProperty]
	private string? _errorMessage;

	[ObservableProperty]
	private string _searchText = string.Empty;

	public MainViewModel(
		INavigator navigator,
		ILapseAuthService authService,
		IApiGlobalService globalService,
		IBuildInfoProvider buildInfoProvider)
	{
		_navigator = navigator;
		_authService = authService;
		_globalService = globalService;
		_buildInfo = buildInfoProvider.GetBuildInfo();

		RefreshCommand = new AsyncRelayCommand(LoadAsync);
		LogoutCommand = new AsyncRelayCommand(LogoutAsync);
		OpenRecordingCommand = new AsyncRelayCommand(() => _navigator.NavigateViewModelAsync<RecordingViewModel>(this));
		OpenTimelapseCommand = new AsyncRelayCommand<TimelapseCardViewModel>(OpenTimelapseAsync);
	}

	public ObservableCollection<LeaderboardEntryViewModel> LeaderboardEntries { get; } = [];

	public ObservableCollection<TimelapseCardViewModel> ExploreTimelapses { get; } = [];

	public string SearchPlaceholder => "Search";

	public bool IsSearchEnabled => false;

	public bool IsWebPlatform => OperatingSystem.IsBrowser();

	public string FooterText => _buildInfo.FullFooterText;

	public string WebFooterText => _buildInfo.WebFooterText;

	public string GreetingTitle => "Welcome to Elapsed, Hack Club's timelapse tracking tool!";

	public string GreetingSubtitle => "Sign in to start tracking your own time with Elapsed";

	public IAsyncRelayCommand RefreshCommand { get; }

	public IAsyncRelayCommand LogoutCommand { get; }

	public IAsyncRelayCommand OpenRecordingCommand { get; }

	public IAsyncRelayCommand<TimelapseCardViewModel> OpenTimelapseCommand { get; }

	public async Task InitializeAsync()
	{
		if (LeaderboardEntries.Count == 0 && ExploreTimelapses.Count == 0)
		{
			await LoadAsync();
		}
	}

	private async Task LoadAsync()
	{
		IsLoading = true;
		ErrorMessage = null;

		try
		{
			var leaderboardTask = _globalService.GetWeeklyLeaderboardAsync();
			var recentTask = _globalService.GetRecentTimelapsesAsync();

			var leaderboardResult = await leaderboardTask;
			var recentResult = await recentTask;

			LeaderboardEntries.Clear();
			if (leaderboardResult.IsSuccess && leaderboardResult.Value is not null)
			{
				foreach (var entry in leaderboardResult.Value)
				{
					LeaderboardEntries.Add(LeaderboardEntryViewModel.FromModel(entry));
				}
			}

			ExploreTimelapses.Clear();
			if (recentResult.IsSuccess && recentResult.Value is not null)
			{
				foreach (var timelapse in recentResult.Value)
				{
					ExploreTimelapses.Add(TimelapseCardViewModel.FromModel(timelapse));
				}
			}

			if (!leaderboardResult.IsSuccess || !recentResult.IsSuccess)
			{
				ErrorMessage = leaderboardResult.ErrorMessage ?? recentResult.ErrorMessage ?? "Some content could not be loaded.";
			}
		}
		finally
		{
			IsLoading = false;
		}
	}

	private async Task LogoutAsync()
	{
		await _authService.LogoutAsync();
		await _navigator.NavigateViewModelAsync<LoginViewModel>(this, qualifier: Qualifiers.ClearBackStack);
	}

	private async Task OpenTimelapseAsync(TimelapseCardViewModel? card)
	{
		if (card is null || string.IsNullOrWhiteSpace(card.TimelapseId))
		{
			return;
		}

		await _navigator.NavigateViewModelAsync<VideoViewModel>(this);
	}

	public sealed class LeaderboardEntryViewModel
	{
		public string Name { get; init; } = string.Empty;
		public string Handle { get; init; } = string.Empty;
		public string WeeklyText { get; init; } = string.Empty;
		public Uri? ProfilePictureUrl { get; init; }

		public static LeaderboardEntryViewModel FromModel(LeaderboardEntry entry)
		{
			var seconds = entry.SecondsThisWeek;
			var hours = (int)(seconds / 3600);
			var minutes = (int)((seconds % 3600) / 60);
			return new LeaderboardEntryViewModel
			{
				Name = entry.User.DisplayName,
				Handle = string.IsNullOrWhiteSpace(entry.User.Handle) ? string.Empty : $"@{entry.User.Handle}",
				WeeklyText = $"{hours}h {minutes}m recorded this week",
				ProfilePictureUrl = entry.User.ProfilePictureUrl,
			};
		}
	}

	public sealed class TimelapseCardViewModel
	{
		public string TimelapseId { get; init; } = string.Empty;
		public string Title { get; init; } = string.Empty;
		public string OwnerText { get; init; } = string.Empty;
		public string MetaText { get; init; } = string.Empty;
		public Uri? ThumbnailUrl { get; init; }
		public Uri? PlaybackUrl { get; init; }
		public string Description { get; init; } = string.Empty;

		public static TimelapseCardViewModel FromModel(Riverside.Elapsed.App.Models.Timelapses.Timelapse timelapse)
		{
			var age = DateTimeOffset.Now - timelapse.CreatedAt;
			var ageText = age.TotalDays >= 1
				? $"{(int)age.TotalDays} days ago"
				: age.TotalHours >= 1
					? $"{(int)age.TotalHours} hours ago"
					: $"{Math.Max(1, (int)age.TotalMinutes)} minutes ago";

			return new TimelapseCardViewModel
			{
				TimelapseId = timelapse.TimelapseId,
				Title = timelapse.Name,
				Description = timelapse.Description,
				OwnerText = $"{timelapse.Owner.DisplayName} · @{timelapse.Owner.Handle}",
				MetaText = ageText,
				ThumbnailUrl = timelapse.ThumbnailUrl,
				PlaybackUrl = timelapse.PlaybackUrl,
			};
		}
	}
}
