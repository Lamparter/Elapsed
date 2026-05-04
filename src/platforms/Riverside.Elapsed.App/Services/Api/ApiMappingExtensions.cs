using System.Globalization;
using Microsoft.Kiota.Abstractions.Serialization;
using Riverside.Elapsed.App.Models.Admin;
using Riverside.Elapsed.App.Models.Auth;
using Riverside.Elapsed.App.Models.Developer;
using Riverside.Elapsed.App.Models.Global;
using Riverside.Elapsed.App.Models.Hackatime;
using Riverside.Elapsed.App.Models.Timelapses;
using Riverside.Elapsed.App.Models.User;
using Riverside.Elapsed.Auth.Token;
using Riverside.Elapsed.Global.ActiveUsers;
using Riverside.Elapsed.Global.RecentTimelapses;
using Riverside.Elapsed.Global.WeeklyLeaderboard;
using Riverside.Elapsed.Hackatime.AllProjects;
using Riverside.Elapsed.Hackatime.MyTimelapsesForProject;
using Riverside.Elapsed.Hackatime.TimelapsesForProject;
using Riverside.Elapsed.User.Myself;
using Riverside.Elapsed.User.Query;
using Riverside.Elapsed.User.QueryByEmail;
using Riverside.Elapsed.User.Update;
using Riverside.Elapsed.User.GetDevices;
using Riverside.Elapsed.User.RegisterDevice;
using Riverside.Elapsed.User.RequestKeyRelay;
using Riverside.Elapsed.User.ReceiveKeyRelay;
using Riverside.Elapsed.User.QueryKeyRelayRequest;
using Riverside.Elapsed.User.GetTotalTimelapseTime;
using Riverside.Elapsed.User.HackatimeProjects;
using Riverside.Elapsed.User.ProvideKeyRelay;
using Riverside.Elapsed.User.DenyKeyRelay;
using Riverside.Elapsed.User.EmitHeartbeat;
using Riverside.Elapsed.Timelapse.Query;
using Riverside.Elapsed.Timelapse.MyPublishedTimelapses;
using Riverside.Elapsed.Timelapse.FindByUser;
using Riverside.Elapsed.DraftTimelapse.FindByUser;
using Riverside.Elapsed.DraftTimelapse.Query;
using Riverside.Elapsed.DraftTimelapse.Legacy;
using Riverside.Elapsed.Comment.Create;
using Riverside.Elapsed.Admin.Stats;
using Riverside.Elapsed.Admin.List;
using Riverside.Elapsed.Admin.Update;
using Riverside.Elapsed.Admin.Search;
using Riverside.Elapsed.Admin.Export;
using Riverside.Elapsed.Admin.RecalculateDurations;
using Riverside.Elapsed.Admin.ProgramKey.List;
using Riverside.Elapsed.Admin.ProgramKey.Create;
using Riverside.Elapsed.Admin.ProgramKey.Rotate;
using Riverside.Elapsed.Admin.ProgramKey.Update;
using Riverside.Elapsed.Admin.ProgramKey.Revoke;
using Riverside.Elapsed.Developer.GetAllOwnedApps;
using Riverside.Elapsed.Developer.GetAllApps;
using Riverside.Elapsed.Developer.AppByClientId;
using Riverside.Elapsed.Developer.CreateApp;
using Riverside.Elapsed.Developer.UpdateApp;
using Riverside.Elapsed.Developer.RotateAppSecret;
using Riverside.Elapsed.Developer.GetOwnedOAuthGrants;
using Riverside.Elapsed.Developer.RevokeOAuthGrant;

namespace Riverside.Elapsed.App.Services.Api;

public static class ApiMappingExtensions
{

	public static UserDetails MapUserDetails(UserMyselfGetResponseMember1_data_userMember1 user)
		=> new()
		{
			UserId = user.Id ?? string.Empty,
			CreatedAt = UnixMillisToDateTimeOffset((long?)user.CreatedAt),
			Handle = user.Handle ?? string.Empty,
			DisplayName = user.DisplayName ?? string.Empty,
			ProfilePictureUrl = new Uri(user.ProfilePictureUrl ?? string.Empty, UriKind.Absolute),
			Bio = user.Bio ?? string.Empty,
			Urls = (user.Urls ?? []).Select(url => new Uri(url, UriKind.Absolute)).ToArray(),
			HackatimeId = user.HackatimeId?.String,
			SlackId = user.SlackId?.String,
		};

	public static UserDetails? MapUserDetails(UntypedNode? node)
	{
		if (node is not UntypedObject untyped)
		{
			return null;
		}

		var values = untyped.GetValue();
		return new UserDetails
		{
			UserId = GetString(values, "id"),
			CreatedAt = UnixMillisToDateTimeOffset(GetLong(values, "createdAt")),
			Handle = GetString(values, "handle"),
			DisplayName = GetString(values, "displayName"),
			ProfilePictureUrl = new Uri(GetString(values, "profilePictureUrl"), UriKind.Absolute),
			Bio = GetString(values, "bio"),
			Urls = GetStringArray(values, "urls")
				.Select(url => new Uri(url, UriKind.Absolute))
				.ToArray(),
			HackatimeId = GetStringOrNull(values, "hackatimeId"),
			SlackId = GetStringOrNull(values, "slackId"),
		};
	}

	public static UserSummary MapUserSummary(GetAllOwnedAppsPostResponseMember1_data_apps_createdByMember1 createdBy)
		=> new()
		{
			UserId = createdBy.Id ?? string.Empty,
			Handle = createdBy.Handle ?? string.Empty,
			DisplayName = createdBy.DisplayName ?? string.Empty,
			ProfilePictureUrl = null,
		};

	public static UserSummary MapUserSummary(ListPostResponseMember1_data_keys_createdBy createdBy)
		=> new()
		{
			UserId = createdBy.Id ?? string.Empty,
			Handle = createdBy.Handle ?? string.Empty,
			DisplayName = createdBy.DisplayName ?? string.Empty,
			ProfilePictureUrl = null,
		};

	public static UserSummary MapUserSummary(WeeklyLeaderboardGetResponseMember1_data_leaderboard entry)
		=> new()
		{
			UserId = entry.Id ?? string.Empty,
			Handle = entry.Handle ?? string.Empty,
			DisplayName = entry.DisplayName ?? string.Empty,
			ProfilePictureUrl = Uri.TryCreate(entry.Pfp, UriKind.Absolute, out var uri) ? uri : null,
		};

	public static UserSummary MapUserSummary(RecentTimelapsesGetResponseMember1_data_timelapses_owner owner)
		=> new()
		{
			UserId = owner.Id ?? string.Empty,
			Handle = owner.Handle ?? string.Empty,
			DisplayName = owner.DisplayName ?? string.Empty,
			ProfilePictureUrl = Uri.TryCreate(owner.ProfilePictureUrl, UriKind.Absolute, out var uri) ? uri : null,
		};

	public static UserSummary MapUserSummary(RecentTimelapsesGetResponseMember1_data_timelapses_comments_author author)
		=> new()
		{
			UserId = author.Id ?? string.Empty,
			Handle = author.Handle ?? string.Empty,
			DisplayName = author.DisplayName ?? string.Empty,
			ProfilePictureUrl = Uri.TryCreate(author.ProfilePictureUrl, UriKind.Absolute, out var uri) ? uri : null,
		};

	public static LeaderboardEntry MapLeaderboardEntry(WeeklyLeaderboardGetResponseMember1_data_leaderboard entry)
		=> new()
		{
			User = new User
			{
				UserId = entry.Id ?? string.Empty,
				Handle = entry.Handle ?? string.Empty,
				DisplayName = entry.DisplayName ?? string.Empty,
				ProfilePictureUrl = Uri.TryCreate(entry.Pfp, UriKind.Absolute, out var uri)
					? uri
					: new Uri("https://example.com", UriKind.Absolute),
				Bio = string.Empty,
				Urls = Array.Empty<Uri>(),
				HackatimeId = null,
				SlackId = null,
			},
			SecondsThisWeek = entry.SecondsThisWeek ?? 0,
		};

	public static User MapTimelapseUser(RecentTimelapsesGetResponseMember1_data_timelapses_owner owner)
		=> new()
		{
			UserId = owner.Id ?? string.Empty,
			Handle = owner.Handle ?? string.Empty,
			DisplayName = owner.DisplayName ?? string.Empty,
			ProfilePictureUrl = Uri.TryCreate(owner.ProfilePictureUrl, UriKind.Absolute, out var uri) ? uri : new Uri("https://example.com", UriKind.Absolute),
			Bio = owner.Bio ?? string.Empty,
			Urls = (owner.Urls ?? []).Select(url => new Uri(url, UriKind.Absolute)).ToArray(),
			HackatimeId = owner.HackatimeId?.String,
			SlackId = owner.SlackId?.String,
		};

	public static User MapTimelapseUser(RecentTimelapsesGetResponseMember1_data_timelapses_comments_author author)
		=> new()
		{
			UserId = author.Id ?? string.Empty,
			Handle = author.Handle ?? string.Empty,
			DisplayName = author.DisplayName ?? string.Empty,
			ProfilePictureUrl = Uri.TryCreate(author.ProfilePictureUrl, UriKind.Absolute, out var uri) ? uri : new Uri("https://example.com", UriKind.Absolute),
			Bio = author.Bio ?? string.Empty,
			Urls = (author.Urls ?? []).Select(url => new Uri(url, UriKind.Absolute)).ToArray(),
			HackatimeId = author.HackatimeId?.String,
			SlackId = author.SlackId?.String,
		};

	public static Comment MapComment(RecentTimelapsesGetResponseMember1_data_timelapses_comments comment)
		=> new()
		{
			CommentId = comment.Id ?? string.Empty,
			Content = comment.Content ?? string.Empty,
			Author = MapTimelapseUser(comment.Author),
			CreatedAt = UnixMillisToDateTimeOffset((long?)comment.CreatedAt),
		};

	public static Timelapse MapTimelapse(RecentTimelapsesGetResponseMember1_data_timelapses timelapse)
		=> new()
		{
			TimelapseId = timelapse.Id ?? string.Empty,
			Name = timelapse.Name ?? string.Empty,
			Description = timelapse.Description ?? string.Empty,
			Visibility = MapVisibility(timelapse.Visibility?.ToString()),
			CreatedAt = UnixMillisToDateTimeOffset((long?)timelapse.CreatedAt),
			Owner = MapTimelapseUser(timelapse.Owner),
			Comments = (timelapse.Comments ?? []).Select(MapComment).ToArray(),
			PlaybackUrl = Uri.TryCreate(timelapse.PlaybackUrl?.String, UriKind.Absolute, out var playback) ? playback : null,
			ThumbnailUrl = Uri.TryCreate(timelapse.ThumbnailUrl?.String, UriKind.Absolute, out var thumbnail) ? thumbnail : null,
			DurationSeconds = timelapse.Duration ?? 0,
			HackatimeProject = timelapse.Private?.HackatimeProject?.String,
			SourceDraftId = timelapse.Private?.SourceDraftId?.String,
		};

	public static DeveloperApp MapDeveloperApp(GetAllOwnedAppsPostResponseMember1_data_apps app, Func<UserSummary?, User?> createdByResolver)
		=> new()
		{
			AppId = app.Id ?? Guid.Empty,
			Name = app.Name ?? string.Empty,
			Description = app.Description ?? string.Empty,
			HomepageUrl = Uri.TryCreate(app.HomepageUrl, UriKind.Absolute, out var homepage) ? homepage : new Uri("https://example.com", UriKind.Absolute),
			IconUrl = Uri.TryCreate(app.IconUrl, UriKind.Absolute, out var icon) ? icon : null,
			RedirectUris = (app.RedirectUris ?? []).Select(uri => new Uri(uri, UriKind.Absolute)).ToArray(),
			Scopes = app.Scopes?.ToArray() ?? Array.Empty<string>(),
			TrustLevel = MapTrustLevel(app.TrustLevel?.ToString()),
			ClientId = app.ClientId ?? string.Empty,
			CreatedAt = DateTimeOffset.TryParse(app.CreatedAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var createdAt)
				? createdAt
				: DateTimeOffset.MinValue,
			CreatedBy = createdByResolver(app.CreatedBy?.GetAllOwnedAppsPostResponseMember1DataAppsCreatedByMember1 is null
				? null
				: MapUserSummary(app.CreatedBy.GetAllOwnedAppsPostResponseMember1DataAppsCreatedByMember1)),
		};

	public static ProgramKeyMetadata MapProgramKey(ListPostResponseMember1_data_keys key, Func<UserSummary?, UserSummary> createdByResolver)
		=> new()
		{
			KeyId = key.Id ?? Guid.Empty,
			Name = key.Name ?? string.Empty,
			KeyPrefix = key.KeyPrefix ?? string.Empty,
			Scopes = key.Scopes?.ToArray() ?? Array.Empty<string>(),
			CreatedBy = createdByResolver(MapUserSummary(key.CreatedBy)),
			CreatedAt = DateTimeOffset.TryParse(key.CreatedAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var createdAt)
				? createdAt
				: DateTimeOffset.MinValue,
			LastUsedAt = DateTimeOffset.TryParse(key.LastUsedAt?.String, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var lastUsedAt)
				? lastUsedAt
				: null,
			RevokedAt = DateTimeOffset.TryParse(key.RevokedAt?.String, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var revokedAt)
				? revokedAt
				: null,
			ExpiresAt = DateTimeOffset.TryParse(key.ExpiresAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var expiresAt)
				? expiresAt
				: DateTimeOffset.MinValue,
		};

	public static HackatimeProject MapHackatimeProject(HackatimeProjectsGetResponseMember1_data_projects project)
		=> new()
		{
			Name = project.Name ?? string.Empty,
			TotalSeconds = project.Time ?? 0,
		};

	public static Device MapDevice(GetDevicesPostResponseMember1_data_devices device)
		=> new()
		{
			DeviceId = Guid.TryParse(device.Id, out var id) ? id : Guid.Empty,
			Name = device.Name ?? string.Empty,
		};

	public static User MapUser(UserDetails details)
		=> new()
		{
			UserId = details.UserId,
			CreatedAt = details.CreatedAt,
			Handle = details.Handle,
			DisplayName = details.DisplayName,
			ProfilePictureUrl = details.ProfilePictureUrl,
			Bio = details.Bio,
			Urls = details.Urls,
			HackatimeId = details.HackatimeId,
			SlackId = details.SlackId,
		};

	public static User MapUser(UserSummary summary)
		=> new()
		{
			UserId = summary.UserId,
			Handle = summary.Handle,
			DisplayName = summary.DisplayName,
			ProfilePictureUrl = summary.ProfilePictureUrl ?? new Uri("https://example.com", UriKind.Absolute),
			Bio = string.Empty,
			Urls = Array.Empty<Uri>(),
			HackatimeId = null,
			SlackId = null,
		};

	public static OAuthGrant MapOAuthGrant(GetOwnedOAuthGrantsPostResponseMember1_data_grants grant)
		=> new()
		{
			GrantId = grant.Id ?? string.Empty,
			ServiceClientId = grant.ServiceClientId ?? string.Empty,
			ServiceName = grant.ServiceName ?? string.Empty,
			Scopes = grant.Scopes?.ToArray() ?? Array.Empty<string>(),
			CreatedAt = DateTimeOffset.TryParse(grant.CreatedAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var createdAt)
				? createdAt
				: DateTimeOffset.MinValue,
			LastUsedAt = DateTimeOffset.TryParse(grant.LastUsedAt?.String, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var lastUsedAt)
				? lastUsedAt
				: null,
		};

	public static AdminSearchResult MapAdminSearchResult(SearchPostResponseMember1_data_results result)
		=> new()
		{
			Entity = MapEntityType(result.Entity?.ToString()),
			Id = result.Id ?? string.Empty,
			DisplayText = result.DisplayText ?? string.Empty,
		};

	public static AdminStatsSummary MapAdminStats(StatsGetResponseMember1_data data)
		=> new()
		{
			TotalLoggedSeconds = data.TotalLoggedSeconds ?? 0,
			TotalProjects = data.TotalProjects ?? 0,
			TotalUsers = data.TotalUsers ?? 0,
		};

	public static AdminListResponse MapAdminList(ListPostResponseMember1_data data)
		=> new()
		{
			Entity = MapEntityType(data.Entity?.ToString()),
			Rows = data.Rows?.ToArray() ?? Array.Empty<object>(),
			Total = data.Total ?? 0,
			Page = data.Page ?? 0,
			PageSize = data.PageSize ?? 0,
		};

	public static AdminUpdateResult MapAdminUpdateResult(UpdatePatchResponseMember1_data data)
		=> new()
		{
			Entity = MapEntityType(data.Entity?.ToString()),
			Row = data.Row ?? new object(),
		};

	public static AdminExport MapAdminExport(ExportPostResponseMember1_data data)
		=> new()
		{
			Data = data.Data ?? new object(),
		};

	public static ProgramKeyMetadata MapProgramKey(CreatePostResponseMember1_data_key key, Func<UserSummary?, UserSummary> createdByResolver)
		=> new()
		{
			KeyId = key.Id ?? Guid.Empty,
			Name = key.Name ?? string.Empty,
			KeyPrefix = key.KeyPrefix ?? string.Empty,
			Scopes = key.Scopes?.ToArray() ?? Array.Empty<string>(),
			CreatedBy = createdByResolver(MapUserSummary(key.CreatedBy)),
			CreatedAt = DateTimeOffset.TryParse(key.CreatedAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var createdAt)
				? createdAt
				: DateTimeOffset.MinValue,
			LastUsedAt = DateTimeOffset.TryParse(key.LastUsedAt?.String, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var lastUsedAt)
				? lastUsedAt
				: null,
			RevokedAt = DateTimeOffset.TryParse(key.RevokedAt?.String, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var revokedAt)
				? revokedAt
				: null,
			ExpiresAt = DateTimeOffset.TryParse(key.ExpiresAt, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var expiresAt)
				? expiresAt
				: DateTimeOffset.MinValue,
		};

	public static TrustLevel MapTrustLevel(string? trustLevel)
		=> trustLevel?.ToUpperInvariant() switch
		{
			"TRUSTED" => TrustLevel.Trusted,
			_ => TrustLevel.Untrusted,
		};

	public static Visibility MapVisibility(string? visibility)
		=> visibility?.ToUpperInvariant() switch
		{
			"PUBLIC" => Visibility.Public,
			"FAILED_PROCESSING" => Visibility.FailedProcessing,
			_ => Visibility.Unlisted,
		};

	public static EntityType MapEntityType(string? entity)
		=> entity?.ToUpperInvariant() switch
		{
			"USER" => EntityType.User,
			"TIMELAPSE" => EntityType.Timelapse,
			"COMMENT" => EntityType.Comment,
			"DRAFTTIMELAPSE" => EntityType.DraftTimelapse,
			"LEGACYTIMELAPSE" => EntityType.LegacyTimelapse,
			_ => EntityType.User,
		};

	private static string GetString(IReadOnlyDictionary<string, UntypedNode> values, string key)
		=> values.TryGetValue(key, out var node) && node is UntypedString untyped
			? untyped.GetValue() ?? string.Empty
			: string.Empty;

	private static string? GetStringOrNull(IReadOnlyDictionary<string, UntypedNode> values, string key)
	{
		if (!values.TryGetValue(key, out var node))
		{
			return null;
		}

		if (node is UntypedNull)
		{
			return null;
		}

		return node is UntypedString untyped ? untyped.GetValue() : null;
	}

	private static long? GetLong(IReadOnlyDictionary<string, UntypedNode> values, string key)
		=> values.TryGetValue(key, out var node) && node is UntypedInteger untyped
			? untyped.GetValue()
			: null;

	private static IReadOnlyList<string> GetStringArray(IReadOnlyDictionary<string, UntypedNode> values, string key)
	{
		if (!values.TryGetValue(key, out var node) || node is not UntypedArray array)
		{
			return Array.Empty<string>();
		}

		return array.GetValue()
			.OfType<UntypedString>()
			.Select(item => item.GetValue() ?? string.Empty)
			.ToArray();
	}

	private static DateTimeOffset UnixMillisToDateTimeOffset(long? millis)
		=> millis.HasValue ? DateTimeOffset.FromUnixTimeMilliseconds(millis.Value) : DateTimeOffset.MinValue;
}
