using System;

namespace NeoSmart.Hashing
{
    public interface IStreamingHashAlgorithm<R>: IHashAlgorithm<R>
        where R: struct
    {
        R Result { get; }

        void Initialize();
        void Initialize(R seed);

        void Update(ReadOnlySpan<byte> input);
        void Update(byte[] input, int offset, int length)
        {
            Update(input.AsSpan(offset, length));
        }
    }
}
