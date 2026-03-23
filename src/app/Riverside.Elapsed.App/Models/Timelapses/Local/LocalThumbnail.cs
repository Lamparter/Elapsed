using System;
using System.Collections.Generic;
using System.Text;

namespace Riverside.Elapsed.App.Models.Timelapses.Local;

public class LocalThumbnail
{
	public string FilePath;
	public long FileSizeBytes;
	//public ThumbnailEncryptionState Encryption;
	public TusUploadState Upload;
}
