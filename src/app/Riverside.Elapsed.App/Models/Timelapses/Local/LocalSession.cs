namespace Riverside.Elapsed.App.Models.Timelapses.Local;

public class LocalSession
{
	public Guid LocalSessionId;
	public string FilePath;
	public long FileSizeBytes;
	// RecordedAtStart, RecordedAtEnd
	//public SessionEncryptionState Encryption;
	public TusUploadState Upload;
}
