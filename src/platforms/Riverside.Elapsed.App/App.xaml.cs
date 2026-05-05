using System.Diagnostics.CodeAnalysis;
using Riverside.Elapsed.App.Extensions;
using Riverside.Elapsed.App.Services.Api;
using Riverside.Elapsed.App.Services.Auth;
using Riverside.Elapsed.App.Services.Build;
using Riverside.Elapsed.App.Services.Storage;
using Riverside.Elapsed.App.ViewModels;
using Uno.Resizetizer;

namespace Riverside.Elapsed.App;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();
	}

	protected Window? MainWindow { get; private set; }
	protected IHost? Host { get; private set; }

	[SuppressMessage("Trimming", "IL2026", Justification = "Uno app builder usage is trim-safe for configured features.")]
	protected override async void OnLaunched(LaunchActivatedEventArgs args)
	{
		var builder = this.CreateBuilder(args)
			.UseToolkitNavigation()
			.Configure(host => host
#if DEBUG
				.UseEnvironment(Environments.Development)
#endif
				.UseLogging((context, logBuilder) =>
				{
					logBuilder
						.SetMinimumLevel(context.HostingEnvironment.IsDevelopment() ? LogLevel.Information : LogLevel.Warning)
						.CoreLogLevel(LogLevel.Warning);
				}, enableUnoLogging: true)
				.UseSerilog(consoleLoggingEnabled: true, fileLoggingEnabled: true)
				.UseConfiguration(configBuilder =>
				{
					configBuilder.Sources.EmbeddedSource<App>();
					configBuilder.Sources.Section<AppConfig>();
				})
				.UseLocalization()
				.ConfigureServices((context, services) =>
				{
#if DEBUG
					services.AddTransient<DelegatingHandler, DebugHttpHandler>();
#endif
					services.AddSingleton<ILocalJsonStore, LocalJsonStore>();
					services.AddSingleton<IAuthTokenStore, AuthTokenStore>();
					services.AddSingleton<ILapseAuthService, LapseAuthService>();
					services.AddSingleton<IBuildInfoProvider, BuildInfoProvider>();

					services.AddScoped<IApiClientFacade, ApiClientFacade>();
					services.AddScoped<IApiUserService, ApiUserService>();
					services.AddScoped<IApiGlobalService, ApiGlobalService>();
					services.AddScoped<IApiDeveloperService, ApiDeveloperService>();

					services.AddDrafts();
				})
				.UseNavigation(RegisterRoutes)
				.UseSerialization(serialization => serialization.AddSingleton(Constants.SerializerOptions))
			);

		MainWindow = builder.Window;
#if DEBUG
		MainWindow.UseStudio();
#endif
		MainWindow.SetWindowIcon();

		Host = await builder.NavigateAsync<Shell>(initialNavigate: async (services, navigator) =>
		{
			var authService = services.GetRequiredService<ILapseAuthService>();
			var isAuthenticated = await authService.TryRestoreSessionAsync();
			if (isAuthenticated)
			{
				await navigator.NavigateViewModelAsync<MainViewModel>(this, qualifier: Qualifiers.Nested);
				return;
			}

			await navigator.NavigateViewModelAsync<LoginViewModel>(this, qualifier: Qualifiers.Nested);
		});
	}

	private static void RegisterRoutes(IViewRegistry views, IRouteRegistry routes)
	{
		views.Register(
			new ViewMap(ViewModel: typeof(ShellViewModel)),
			new ViewMap<LoginPage, LoginViewModel>(),
			new ViewMap<MainPage, MainViewModel>(),
			new ViewMap<VideoPage, VideoViewModel>(),
			new ViewMap<RecordingPage, RecordingViewModel>()
		);

		routes.Register(
			new RouteMap(
				"",
				View: views.FindByViewModel<ShellViewModel>(),
				Nested:
				[
					new RouteMap("Login", View: views.FindByViewModel<LoginViewModel>()),
					new RouteMap("Main", View: views.FindByViewModel<MainViewModel>(), IsDefault: true),
					new RouteMap("Video", View: views.FindByViewModel<VideoViewModel>()),
					new RouteMap("Recording", View: views.FindByViewModel<RecordingViewModel>()),
				]
			)
		);
	}
}
