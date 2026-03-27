using System.Collections.ObjectModel;
using Riverside.Elapsed.App.Services.Drafts;

namespace Riverside.Elapsed.App.ViewModels.Timelapses.Drafts;

public sealed partial class DraftViewModel : ObservableObject
{
	private readonly ILocalDraftRepository _drafts;
	private readonly INavigator _navigator;

	[ObservableProperty]
	private bool _isLoading;

	[ObservableProperty]
	private string? _errorMessage;

	public ObservableCollection<DraftListItemViewModel> Items { get; }

	public IAsyncRelayCommand RefreshCommand { get; }
	public IAsyncRelayCommand CreateNewDraftCommand { get; }
	public IAsyncRelayCommand<Guid> DeleteDraftCommand { get; }
	public IAsyncRelayCommand<Guid> OpenDraftCommand { get; }

	public DraftsViewModel(ILocalDraftRepository drafts)
	{
		_drafts = drafts;
	}

	public async Task RefreshAsync()
	{
		try
		{
			ErrorMessage = null;
			IsLoading = true;

			var index = await _drafts.GetIndexAsync().ConfigureAwait(false);
		}
	}
}
