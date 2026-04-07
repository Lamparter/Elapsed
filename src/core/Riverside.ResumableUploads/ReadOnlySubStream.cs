namespace Riverside.ResumableUploads;

internal sealed class ReadOnlySubStream : Stream
{
	private readonly Stream _inner;
	private long _remaining;

	public override bool CanRead => _inner.CanRead;
	public override bool CanSeek => false;
	public override bool CanWrite => false;

	public override long Length => throw new NotSupportedException();
	public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

	public ReadOnlySubStream(Stream inner, long length)
	{
		_inner = inner ?? throw new ArgumentNullException(nameof(inner));
		_remaining = length < 0 ? throw new ArgumentOutOfRangeException(nameof(length)) : length;
	}

	public override int Read(byte[] buffer, int offset, int count)
	{
		if (_remaining <= 0)
			return 0;

		count = (int)Math.Min(count, _remaining);
		var read = _inner.Read(buffer, offset, count);
		_remaining -= read;
		return read;
	}

	public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
	{
		if (_remaining <= 0)
			return 0;

		count = (int)Math.Min(count, _remaining);
		var read = await _inner.ReadAsync(buffer, offset, count, cancellationToken).ConfigureAwait(false);
		_remaining -= read;
		return read;
	}

	public override long Seek(long offset, SeekOrigin origin)
		=> throw new NotSupportedException();

	public override void SetLength(long value)
		=> throw new NotSupportedException();

	public override void Write(byte[] buffer, int offset, int count)
		=> throw new NotSupportedException();

	public override void Flush() { }
}
