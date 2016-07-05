using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BacASableWPF4
{
    public class GeneratorStream : Stream
    {
        private readonly Func<long, byte> _generator;

        public GeneratorStream(long length, Func<long, byte> generator)
        {
            Length = length;
            _generator = generator;
            Position = 0;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length { get; }

        public override long Position { get; set; }

        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (Position >= (Length - 1))
                {
                    return i;
                }

                Position++;
                buffer[offset + i] = _generator(Position);
            }

            return count;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }
    }

    public class StreamFromByteEnumerable : Stream
    {
        private readonly IEnumerator<byte> _source;

        public StreamFromByteEnumerable(IEnumerable<byte> source)
        {
            _source = source.GetEnumerator();
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length { get { throw new NotImplementedException(); } }

        public override long Position
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public override void Flush() { throw new NotImplementedException(); }

        public override int Read(byte[] buffer, int offset, int count)
        {
            for (int i = 0; i < count; i++)
            {
                if (!_source.MoveNext())
                {
                    return i;
                }

                buffer[offset + i] = _source.Current;
            }

            return count;

        }

        public override long Seek(long offset, SeekOrigin origin) { throw new NotImplementedException(); }

        public override void SetLength(long value) { throw new NotImplementedException(); }

        public override void Write(byte[] buffer, int offset, int count) { throw new NotImplementedException(); }
    }

    public class StringEnumerableConverter
    {
        private readonly Encoding _encoding;

        public StringEnumerableConverter(Encoding encoding = null)
        {
            _encoding = encoding ?? Encoding.UTF8;
        }

        public IEnumerable<byte> Convert(IEnumerable<string> source)
        {
            return source.SelectMany(s => _encoding.GetBytes(s));
        }
    }
}
