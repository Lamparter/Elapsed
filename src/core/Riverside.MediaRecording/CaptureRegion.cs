namespace Riverside.MediaRecording;

/// <summary>
/// Represents a rectangular capture region in logical pixels.
/// </summary>
/// <param name="Left">The left coordinate of the region.</param>
/// <param name="Top">The top coordinate of the region.</param>
/// <param name="Width">The width of the region.</param>
/// <param name="Height">The height of the region.</param>
public readonly record struct CaptureRegion(int Left, int Top, int Width, int Height);
