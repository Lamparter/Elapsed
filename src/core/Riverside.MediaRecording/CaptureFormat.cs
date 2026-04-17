namespace Riverside.MediaRecording;

public sealed class CaptureFormat
{
	public int Width { get; set; }
	public int Height { get; set; }
	public double Framerate { get; set; }
	public string PixelFormat { get; set; }
	public string VideoCodec { get; set; }
	public string AudioCodec { get; set; }
	public int Bitrate { get; set; } // kbps
}
