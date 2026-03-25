namespace Riverside.Elapsed.App.Models.Timelapses.Local;

public class RemoteDraftSync
{
	public string DraftTimelapseId;
	public byte[] IvBytes; // IvHex
	public List<string> SessionUploadTokens;
	public string ThumbnailUploadToken;
}
