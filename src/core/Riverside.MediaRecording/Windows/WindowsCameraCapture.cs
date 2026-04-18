extern alias user32;

using System.Runtime.InteropServices;
using OwlCore.Storage;
using UserPInvoke = user32::Windows.Win32.PInvoke;
using user32::Windows.Win32.Foundation;
using user32::Windows.Win32.UI.WindowsAndMessaging;

namespace Riverside.MediaRecording.Windows;

/// <summary>
/// Provides Windows camera capture using AVICap APIs.
/// </summary>
public sealed class WindowsCameraCapture : ICameraCapturable
{
	private const uint WmUser = 0x0400;
	private const uint WmCapStart = WmUser;
	private const uint WmCapDriverConnect = WmCapStart + 10;
	private const uint WmCapDriverDisconnect = WmCapStart + 11;
	private const uint WmCapFileSetCaptureFileW = WmCapStart + 20;
	private const uint WmCapSequence = WmCapStart + 62;
	private const uint WmCapStop = WmCapStart + 68;

	private static readonly RecordableDeviceCapabilities _cameraCapabilities = new()
	{
		SupportsVideoCapture = true,
		SupportsAudioCapture = true,
		SupportsRegionCapture = false,
		SupportsSourceSwitching = true,
	};

	private readonly List<RecordableDevice> _sources = [];
	private readonly List<IVideoCaptureSession> _sessions = [];
	private readonly Dictionary<Guid, int> _deviceIndexes = [];

	/// <inheritdoc/>
	public IReadOnlyList<RecordableDevice> Sources => _sources;

	/// <inheritdoc/>
	public IReadOnlyList<IVideoCaptureSession> Sessions => _sessions;

	/// <summary>
	/// Initialises a new instance of the <see cref="WindowsCameraCapture"/> class.
	/// </summary>
	public WindowsCameraCapture()
	{
		InitializeSources();
	}

	/// <inheritdoc/>
	public Task RefreshSourcesAsync(CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		InitializeSources();
		return Task.CompletedTask;
	}

	/// <inheritdoc/>
	public Task<IVideoCaptureSession> CreateRecordingSessionAsync(
		RecordableDevice source,
		CaptureFormat? format = null,
		CaptureRegion? region = null,
		AudioCaptureOptions? audio = null,
		IFile? outputFile = null,
		CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();

		if (source.DeviceType != DeviceType.Camera)
			throw new ArgumentException("The source must be a camera source.", nameof(source));

		if (!_deviceIndexes.TryGetValue(source.Id, out var cameraIndex))
			throw new ArgumentException("The specified camera source is not known to this provider.", nameof(source));

		var session = new WindowsCameraCaptureSession(source, cameraIndex, format, audio, outputFile);
		_sessions.Add(session);

		return Task.FromResult<IVideoCaptureSession>(session);
	}

	/// <inheritdoc/>
	public ValueTask DisposeAsync()
	{
		_sources.Clear();
		_sessions.Clear();
		_deviceIndexes.Clear();
		return ValueTask.CompletedTask;
	}

	private void InitializeSources()
	{
		_sources.Clear();
		_deviceIndexes.Clear();

		for (var index = 0; index < 10; index++)
		{
			if (!ProbeCamera(index))
				continue;

			var bytes = new byte[16];
			BitConverter.GetBytes(index + 1).CopyTo(bytes, 0);
			var id = new Guid(bytes);

			var device = new RecordableDevice(
				Id: id,
				Name: $"Camera {index + 1}",
				DeviceType: DeviceType.Camera,
				Capabilities: _cameraCapabilities);

			_sources.Add(device);
			_deviceIndexes[id] = index;
		}

		if (_sources.Count > 0)
			return;

		var fallbackId = Guid.Parse("33333333-3333-3333-3333-333333333333");
		_sources.Add(new RecordableDevice(
			Id: fallbackId,
			Name: "Default camera",
			DeviceType: DeviceType.Camera,
			Capabilities: _cameraCapabilities));
		_deviceIndexes[fallbackId] = 0;
	}

	private static unsafe bool ProbeCamera(int index)
	{
		var window = UserPInvoke.CreateWindowEx(
			(WINDOW_EX_STYLE)0,
			"avicap32",
			$"Camera probe {index}",
			WINDOW_STYLE.WS_POPUP,
			0,
			0,
			160,
			120,
			HWND.Null,
			null,
			null,
			null);

		if (window.IsNull)
			return false;

		try
		{
			var connected = UserPInvoke.SendMessage(
				window,
				WmCapDriverConnect,
				new WPARAM((nuint)index),
				new LPARAM(nint.Zero));

			if (connected.Value == 0)
				return false;

			UserPInvoke.SendMessage(window, WmCapDriverDisconnect, new WPARAM(0), new LPARAM(nint.Zero));
			return true;
		}
		finally
		{
			UserPInvoke.DestroyWindow(window);
		}
	}

	private sealed class WindowsCameraCaptureSession : IVideoCaptureSession
	{
		private readonly object _gate = new();
		private readonly int _cameraIndex;
		private readonly IFile? _outputFile;
		private readonly string _temporaryCapturePath;

		private HWND _captureWindow;
		private DateTimeOffset _startedAt;
		private DateTimeOffset _endedAt;

		public WindowsCameraCaptureSession(
			RecordableDevice source,
			int cameraIndex,
			CaptureFormat? format,
			AudioCaptureOptions? audio,
			IFile? outputFile)
		{
			Id = Guid.NewGuid();
			Source = source;
			Format = format;
			Audio = audio;
			_outputFile = outputFile;
			_cameraIndex = cameraIndex;
			_temporaryCapturePath = Path.ChangeExtension(Path.GetTempFileName(), ".avi");
			Status = RecordingStatus.NotStarted;
		}

		public Guid Id { get; }

		public RecordingStatus Status { get; private set; }

		public TimeSpan Duration
		{
			get
			{
				if (_startedAt == default)
					return TimeSpan.Zero;

				var end = _endedAt == default ? DateTimeOffset.UtcNow : _endedAt;
				return end - _startedAt;
			}
		}

		public RecordableDevice Source { get; private set; }

		public IFile? OutputFile => _outputFile;

		public bool CanSwitchSource => true;

		public CaptureFormat? Format { get; }

		public CaptureRegion? Region => null;

		public AudioCaptureOptions? Audio { get; }

		public Task StartAsync(CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();

			lock (_gate)
			{
				if (Status is not RecordingStatus.NotStarted)
					throw new InvalidOperationException("The session has already been started.");

				Status = RecordingStatus.Starting;
				_captureWindow = CreateCaptureWindow(_cameraIndex);
				ConfigureCaptureFile(_captureWindow, _temporaryCapturePath);
				StartSequence(_captureWindow);
				_startedAt = DateTimeOffset.UtcNow;
				Status = RecordingStatus.Active;
			}

			return Task.CompletedTask;
		}

		public Task PauseAsync(CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();

			lock (_gate)
			{
				if (Status is not RecordingStatus.Active)
					throw new InvalidOperationException("Only active sessions can be paused.");

				Status = RecordingStatus.Pausing;
				StopSequence(_captureWindow);
				Status = RecordingStatus.Paused;
			}

			return Task.CompletedTask;
		}

		public Task ResumeAsync(CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();

			lock (_gate)
			{
				if (Status is not RecordingStatus.Paused)
					throw new InvalidOperationException("Only paused sessions can be resumed.");

				Status = RecordingStatus.Resuming;
				StartSequence(_captureWindow);
				Status = RecordingStatus.Active;
			}

			return Task.CompletedTask;
		}

		public Task SwitchSourceAsync(RecordableDevice source, CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (source.DeviceType != DeviceType.Camera)
				throw new ArgumentException("The source must be a camera source.", nameof(source));

			Source = source;
			return Task.CompletedTask;
		}

		public async Task<CapturedVideo> StopAsync(CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();

			lock (_gate)
			{
				if (Status is RecordingStatus.NotStarted)
					throw new InvalidOperationException("The session has not been started.");

				if (Status is RecordingStatus.Stopped)
					throw new InvalidOperationException("The session has already been stopped.");

				Status = RecordingStatus.Stopping;
				StopSequence(_captureWindow);
				ReleaseCaptureWindow(_captureWindow);
				_captureWindow = HWND.Null;
				_endedAt = DateTimeOffset.UtcNow;
			}

			if (_outputFile is not null)
			{
				await using var destination = await _outputFile.OpenWriteAsync().ConfigureAwait(false);
				await using var sourceStream = File.OpenRead(_temporaryCapturePath);
				await sourceStream.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
				await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
			}

			if (File.Exists(_temporaryCapturePath))
				File.Delete(_temporaryCapturePath);

			Status = RecordingStatus.Stopped;

			return new CapturedVideo
			{
				File = _outputFile,
				StartedAt = _startedAt,
				EndedAt = _endedAt,
				Format = Format,
				Region = null,
				Audio = Audio,
			};
		}

		private static unsafe HWND CreateCaptureWindow(int cameraIndex)
		{
			var window = UserPInvoke.CreateWindowEx(
				(WINDOW_EX_STYLE)0,
				"avicap32",
				$"Camera capture {cameraIndex}",
				WINDOW_STYLE.WS_POPUP,
				0,
				0,
				640,
				480,
				HWND.Null,
				null,
				null,
				null);

			if (window.IsNull)
				throw new InvalidOperationException("Unable to create the camera capture window.");

			var connected = UserPInvoke.SendMessage(
				window,
				WmCapDriverConnect,
				new WPARAM((nuint)cameraIndex),
				new LPARAM(nint.Zero));

			if (connected.Value == 0)
			{
				UserPInvoke.DestroyWindow(window);
				throw new InvalidOperationException("Unable to connect to the selected camera source.");
			}

			return window;
		}

		private static void ConfigureCaptureFile(HWND captureWindow, string filePath)
		{
			var pathPointer = Marshal.StringToHGlobalUni(filePath);
			try
			{
				var configured = UserPInvoke.SendMessage(
					captureWindow,
					WmCapFileSetCaptureFileW,
					new WPARAM(0),
					new LPARAM(pathPointer));

				if (configured.Value == 0)
					throw new InvalidOperationException("Unable to configure camera capture output file.");
			}
			finally
			{
				Marshal.FreeHGlobal(pathPointer);
			}
		}

		private static void StartSequence(HWND captureWindow)
		{
			var started = UserPInvoke.SendMessage(captureWindow, WmCapSequence, new WPARAM(0), new LPARAM(nint.Zero));
			if (started.Value == 0)
				throw new InvalidOperationException("Unable to start camera capture sequence.");
		}

		private static void StopSequence(HWND captureWindow)
		{
			if (captureWindow.IsNull)
				return;

			UserPInvoke.SendMessage(captureWindow, WmCapStop, new WPARAM(0), new LPARAM(nint.Zero));
		}

		private static void ReleaseCaptureWindow(HWND captureWindow)
		{
			if (captureWindow.IsNull)
				return;

			UserPInvoke.SendMessage(captureWindow, WmCapDriverDisconnect, new WPARAM(0), new LPARAM(nint.Zero));
			UserPInvoke.DestroyWindow(captureWindow);
		}
	}
}
