using System.IO.Compression;
using OwlCore.Storage;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace Riverside.MediaRecording.Windows;

/// <summary>
/// Provides Windows screen capture using GDI.
/// </summary>
public sealed class WindowsScreenCapture : IScreenCapturable
{
	private static readonly RecordableDeviceCapabilities _screenCapabilities = new()
	{
		SupportsVideoCapture = true,
		SupportsAudioCapture = false,
		SupportsRegionCapture = true,
		SupportsSourceSwitching = true,
	};

	private readonly List<RecordableDevice> _sources = [];
	private readonly List<IVideoCaptureSession> _sessions = [];

	/// <inheritdoc/>
	public IReadOnlyList<RecordableDevice> Sources => _sources;

	/// <inheritdoc/>
	public IReadOnlyList<IVideoCaptureSession> Sessions => _sessions;

	/// <summary>
	/// Initialises a new instance of the <see cref="WindowsScreenCapture"/> class.
	/// </summary>
	public WindowsScreenCapture()
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

		if (source.DeviceType is not DeviceType.Display and not DeviceType.Window and not DeviceType.Region)
			throw new ArgumentException("The source must be a screen-based source.", nameof(source));

		if (_sources.All(x => x.Id != source.Id))
			throw new ArgumentException("The specified source is not known to this provider.", nameof(source));

		var session = new WindowsScreenCaptureSession(source, format, region, audio, outputFile);
		_sessions.Add(session);

		return Task.FromResult<IVideoCaptureSession>(session);
	}

	/// <inheritdoc/>
	public ValueTask DisposeAsync()
	{
		_sources.Clear();
		_sessions.Clear();
		return ValueTask.CompletedTask;
	}

	private void InitializeSources()
	{
		_sources.Clear();

		var primaryDisplay = new RecordableDevice(
			Id: Guid.Parse("11111111-1111-1111-1111-111111111111"),
			Name: "Primary display",
			DeviceType: DeviceType.Display,
			Capabilities: _screenCapabilities);

		var desktopRegion = new RecordableDevice(
			Id: Guid.Parse("22222222-2222-2222-2222-222222222222"),
			Name: "Desktop region",
			DeviceType: DeviceType.Region,
			Capabilities: _screenCapabilities);

		_sources.Add(primaryDisplay);
		_sources.Add(desktopRegion);
	}

	private sealed class WindowsScreenCaptureSession : IVideoCaptureSession
	{
		private readonly object _gate = new();
		private readonly List<ScreenFrame> _frames = [];
		private readonly IFile? _outputFile;

		private CancellationTokenSource? _captureCts;
		private Task? _captureTask;
		private DateTimeOffset _startedAt;
		private DateTimeOffset _endedAt;

		public WindowsScreenCaptureSession(
			RecordableDevice source,
			CaptureFormat? format,
			CaptureRegion? region,
			AudioCaptureOptions? audio,
			IFile? outputFile)
		{
			Id = Guid.NewGuid();
			Source = source;
			Format = format;
			Region = region;
			Audio = audio;
			_outputFile = outputFile;
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

		public CaptureRegion? Region { get; }

		public AudioCaptureOptions? Audio { get; }

		public Task StartAsync(CancellationToken cancellationToken = default)
		{
			lock (_gate)
			{
				if (Status is not RecordingStatus.NotStarted)
					throw new InvalidOperationException("The session has already been started.");

				Status = RecordingStatus.Starting;
				_startedAt = DateTimeOffset.UtcNow;
				_captureCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
				_captureTask = CaptureLoopAsync(_captureCts.Token);
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
				Status = RecordingStatus.Active;
			}

			return Task.CompletedTask;
		}

		public Task SwitchSourceAsync(RecordableDevice source, CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();

			if (source.DeviceType is not DeviceType.Display and not DeviceType.Window and not DeviceType.Region)
				throw new ArgumentException("The source must be a screen-based source.", nameof(source));

			Source = source;
			return Task.CompletedTask;
		}

		public async Task<CapturedVideo> StopAsync(CancellationToken cancellationToken = default)
		{
			Task? captureTask;

			lock (_gate)
			{
				if (Status is RecordingStatus.Stopped)
					throw new InvalidOperationException("The session has already been stopped.");

				if (Status is RecordingStatus.NotStarted)
					throw new InvalidOperationException("The session has not been started.");

				Status = RecordingStatus.Stopping;
				_captureCts?.Cancel();
				captureTask = _captureTask;
			}

			if (captureTask is not null)
				await captureTask.ConfigureAwait(false);

			_endedAt = DateTimeOffset.UtcNow;

			if (_outputFile is not null)
			{
				await using var destination = await _outputFile.OpenWriteAsync().ConfigureAwait(false);
				await WriteArchiveAsync(destination, cancellationToken).ConfigureAwait(false);
				await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
			}

			Status = RecordingStatus.Stopped;

			return new CapturedVideo
			{
				File = _outputFile,
				StartedAt = _startedAt,
				EndedAt = _endedAt,
				Format = Format,
				Region = Region,
				Audio = Audio,
			};
		}

		private async Task CaptureLoopAsync(CancellationToken cancellationToken)
		{
			var frameRate = Format?.FrameRate ?? 10d;
			if (frameRate <= 0)
				frameRate = 10d;

			var delay = TimeSpan.FromMilliseconds(Math.Max(15d, 1000d / frameRate));

			while (!cancellationToken.IsCancellationRequested)
			{
				if (Status == RecordingStatus.Active)
				{
					var frame = CaptureScreenFrame(Region);
					lock (_gate)
					{
						_frames.Add(frame);
					}
				}

				try
				{
					await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
				}
				catch (OperationCanceledException)
				{
					break;
				}
			}
		}

		private async Task WriteArchiveAsync(Stream stream, CancellationToken cancellationToken)
		{
			using var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true);

			var index = 0;
			List<ScreenFrame> frames;
			lock (_gate)
			{
				frames = [.. _frames];
			}

			foreach (var frame in frames)
			{
				var entry = archive.CreateEntry($"frame-{index:D6}.bmp", CompressionLevel.Fastest);
				await using var entryStream = entry.Open();
				await entryStream.WriteAsync(frame.BmpBytes, cancellationToken).ConfigureAwait(false);
				index++;
			}

			var manifest = archive.CreateEntry("manifest.txt", CompressionLevel.Fastest);
			await using var manifestStream = manifest.Open();
			await using var writer = new StreamWriter(manifestStream);
			await writer.WriteLineAsync($"startedAt={_startedAt:O}").ConfigureAwait(false);
			await writer.WriteLineAsync($"endedAt={_endedAt:O}").ConfigureAwait(false);
			await writer.WriteLineAsync($"frameCount={frames.Count}").ConfigureAwait(false);
			await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
		}

		private static unsafe ScreenFrame CaptureScreenFrame(CaptureRegion? requestedRegion)
		{
			var region = requestedRegion ?? GetDesktopRegion();

			var desktop = PInvoke.GetDesktopWindow();
			if (desktop.IsNull)
				throw new InvalidOperationException("Unable to resolve the desktop window.");

			var sourceDc = PInvoke.GetDC(desktop);
			if (sourceDc.IsNull)
				throw new InvalidOperationException("Unable to acquire the desktop device context.");

			var memoryDc = PInvoke.CreateCompatibleDC(sourceDc);
			if (memoryDc.IsNull)
			{
				PInvoke.ReleaseDC(desktop, sourceDc);
				throw new InvalidOperationException("Unable to create a compatible memory device context.");
			}

			var bitmap = PInvoke.CreateCompatibleBitmap(sourceDc, region.Width, region.Height);
			if (bitmap.IsNull)
			{
				PInvoke.DeleteDC(memoryDc);
				PInvoke.ReleaseDC(desktop, sourceDc);
				throw new InvalidOperationException("Unable to create a compatible bitmap.");
			}

			var oldObject = PInvoke.SelectObject(memoryDc, (HGDIOBJ)bitmap);
			try
			{
				var rop = (ROP_CODE)0x40CC0020u;
				var bitBlt = PInvoke.BitBlt(
					memoryDc,
					0,
					0,
					region.Width,
					region.Height,
					sourceDc,
					region.Left,
					region.Top,
					rop);

				if (!bitBlt)
					throw new InvalidOperationException("BitBlt failed while capturing a frame.");

				var pixels = new byte[region.Width * region.Height * 4];
				BITMAPINFO info = default;
				info.bmiHeader.biSize = (uint)sizeof(BITMAPINFOHEADER);
				info.bmiHeader.biWidth = region.Width;
				info.bmiHeader.biHeight = -region.Height;
				info.bmiHeader.biPlanes = 1;
				info.bmiHeader.biBitCount = 32;
				info.bmiHeader.biCompression = 0;

				fixed (byte* pixelPtr = pixels)
				{
					var lineCount = PInvoke.GetDIBits(
						memoryDc,
						bitmap,
						0,
						(uint)region.Height,
						pixelPtr,
						&info,
						DIB_USAGE.DIB_RGB_COLORS);

					if (lineCount == 0)
						throw new InvalidOperationException("GetDIBits failed while reading captured frame data.");
				}

				return new ScreenFrame(DateTimeOffset.UtcNow, CreateBitmapFileBytes(region.Width, region.Height, pixels));
			}
			finally
			{
				PInvoke.SelectObject(memoryDc, oldObject);
				PInvoke.DeleteObject((HGDIOBJ)bitmap);
				PInvoke.DeleteDC(memoryDc);
				PInvoke.ReleaseDC(desktop, sourceDc);
			}
		}

		private static CaptureRegion GetDesktopRegion()
		{
			var desktop = PInvoke.GetDesktopWindow();
			if (desktop.IsNull)
				throw new InvalidOperationException("Unable to resolve the desktop window.");

			if (!PInvoke.GetWindowRect(desktop, out var rect))
				throw new InvalidOperationException("Unable to query the desktop bounds.");

			return new CaptureRegion(rect.left, rect.top, rect.right - rect.left, rect.bottom - rect.top);
		}

		private static byte[] CreateBitmapFileBytes(int width, int height, byte[] pixelData)
		{
			using var stream = new MemoryStream(capacity: 54 + pixelData.Length);
			using var writer = new BinaryWriter(stream);

			const int fileHeaderSize = 14;
			const int infoHeaderSize = 40;
			var pixelOffset = fileHeaderSize + infoHeaderSize;
			var fileSize = pixelOffset + pixelData.Length;

			writer.Write((ushort)0x4D42);
			writer.Write(fileSize);
			writer.Write((ushort)0);
			writer.Write((ushort)0);
			writer.Write(pixelOffset);

			writer.Write(infoHeaderSize);
			writer.Write(width);
			writer.Write(-height);
			writer.Write((ushort)1);
			writer.Write((ushort)32);
			writer.Write(0);
			writer.Write(pixelData.Length);
			writer.Write(0);
			writer.Write(0);
			writer.Write(0);
			writer.Write(0);

			writer.Write(pixelData);
			writer.Flush();

			return stream.ToArray();
		}

		private readonly record struct ScreenFrame(DateTimeOffset Timestamp, byte[] BmpBytes);
	}
}
