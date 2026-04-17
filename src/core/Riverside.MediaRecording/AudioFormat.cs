namespace Riverside.MediaRecording;

public sealed class AudioFormat
{
	public bool IncludeMicrophone { get; set; }
	public bool IncludeSystemAudio { get; set; }
	public string? MicrophoneDeviceId { get; set; }
	public string? SystemAudioDeviceId { get; set; }
	public int Bitrate { get; set; } // kbps
}
