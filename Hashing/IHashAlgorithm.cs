using System;

namespace NeoSmart.Hashing
{
    public interface IHashAlgorithm<R>
    {
        UInt32 HashLengthBits { get; }
        R Hash(byte[] input, int offset, int length);
    }
}
