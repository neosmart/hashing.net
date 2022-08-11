using System;

namespace NeoSmart.Hashing
{
    public interface IHashAlgorithm<R>
        where R: struct
    {
        UInt32 HashLengthBits { get; }

        R Hash(ReadOnlySpan<byte> input);
        R Hash(byte[] input, int offset, int length)
        {
            return Hash(input.AsSpan(offset, length));
        }
    }
}
