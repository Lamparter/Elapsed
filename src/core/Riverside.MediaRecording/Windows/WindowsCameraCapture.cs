using System.Runtime.InteropServices;
using OwlCore.Storage;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.UI.WindowsAndMessaging;

namespace Riverside.MediaRecording.Windows;

/// <summary>
/// Provides Windows camera capture using AVICap APIs.
/// </summary>
public sealed class WindowsCameraCapture : ICameraCapturable
{
	private static readonly RecordableDeviceCapabilities _cameraCapabilities = new()
	{
		SupportsVideoCapture = true,
		SupportsAudioCapture = true,
		SupportsRegionCapture = false,
		SupportsSourceSwitching = false,
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

		if (outputFile is null)
			throw new ArgumentNullException(nameof(outputFile), "An output file is required to persist the captured video.");

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

	private static bool ProbeCamera(int index)
	{
		var window = PInvoke.capCreateCaptureWindow(
			$"Camera probe {index}",
			(uint)WINDOW_STYLE.WS_POPUP,
			0,
			0,
			160,
			120,
			HWND.Null,
			0);

		if (window.IsNull)
			return false;

		try
		{
			var connected = PInvoke.SendMessage(
				window,
				PInvoke.WM_CAP_DRIVER_CONNECT,
				new WPARAM((nuint)index),
				new LPARAM(nint.Zero));

			if (connected.Value == 0)
				return false;

			PInvoke.SendMessage(window, PInvoke.WM_CAP_DRIVER_DISCONNECT, new WPARAM(0), new LPARAM(nint.Zero));
			return true;
		}
		finally
		{
			PInvoke.DestroyWindow(window);
		}
	}

	private sealed class WindowsCameraCaptureSession : IVideoCaptureSession
	{
		private readonly object _gate = new();
		private readonly int _cameraIndex;
		private readonly IFile _outputFile;
		private readonly string _temporaryCapturePath;

		private HWND _captureWindow;
		private DateTimeOffset _startedAt;
		private DateTimeOffset _endedAt;

		public WindowsCameraCaptureSession(
			RecordableDevice source,
			int cameraIndex,
			CaptureFormat? format,
			AudioCaptureOptions? audio,
			IFile outputFile)
		{
			Id = Guid.NewGuid();
			Source = source;
			Format = format;
			Audio = audio;
			_outputFile = outputFile;
			_cameraIndex = cameraIndex;
			_temporaryCapturePath = Path.GetTempFileName();
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

		public bool CanSwitchSource => false;

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
			throw new NotSupportedException("Source switching is not supported by this camera capture implementation.");
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

			try
			{
				await using var destination = await _outputFile.OpenWriteAsync().ConfigureAwait(false);
				await using var sourceStream = File.OpenRead(_temporaryCapturePath);
				await sourceStream.CopyToAsync(destination, cancellationToken).ConfigureAwait(false);
				await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
			}
			finally
			{
				if (File.Exists(_temporaryCapturePath))
					File.Delete(_temporaryCapturePath);
			}

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

		private static HWND CreateCaptureWindow(int cameraIndex)
		{
			var window = PInvoke.capCreateCaptureWindow(
				$"Camera capture {cameraIndex}",
				(uint)WINDOW_STYLE.WS_POPUP,
				0,
				0,
				640,
				480,
				HWND.Null,
				0);

			if (window.IsNull)
				throw new InvalidOperationException($"Unable to create the camera capture window. Win32 error: {Marshal.GetLastWin32Error()}.");

			var connected = PInvoke.SendMessage(
				window,
				PInvoke.WM_CAP_DRIVER_CONNECT,
				new WPARAM((nuint)cameraIndex),
				new LPARAM(nint.Zero));

			if (connected.Value == 0)
			{
				PInvoke.DestroyWindow(window);
				throw new InvalidOperationException("Unable to connect to the selected camera source.");
			}

			return window;
		}

		private static void ConfigureCaptureFile(HWND captureWindow, string filePath)
		{
			var pathPointer = Marshal.StringToHGlobalUni(filePath);
			try
			{
				var configured = PInvoke.SendMessage(
					captureWindow,
					PInvoke.WM_CAP_FILE_SET_CAPTURE_FILE,
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
			var started = PInvoke.SendMessage(captureWindow, PInvoke.WM_CAP_SEQUENCE, new WPARAM(0), new LPARAM(nint.Zero));
			if (started.Value == 0)
				throw new InvalidOperationException("Unable to start camera capture sequence.");
		}

		private static void StopSequence(HWND captureWindow)
		{
			if (captureWindow.IsNull)
				return;

			PInvoke.SendMessage(captureWindow, PInvoke.WM_CAP_STOP, new WPARAM(0), new LPARAM(nint.Zero));
		}

		private static void ReleaseCaptureWindow(HWND captureWindow)
		{
			if (captureWindow.IsNull)
				return;

			PInvoke.SendMessage(captureWindow, PInvoke.WM_CAP_DRIVER_DISCONNECT, new WPARAM(0), new LPARAM(nint.Zero));
			PInvoke.DestroyWindow(captureWindow);
		}
	}
}
