using System;

namespace NeoSmart.Hashing
{
    internal static class HashSingleton<T, R>
        where T: IHashAlgorithm<R>, new()
        where R: struct
    {
        private static readonly T _hashCore;

        public static UInt32 HashLengthBits => _hashCore.HashLengthBits;

        static HashSingleton()
        {
            _hashCore = new T();
        }

        public static R Hash(ReadOnlySpan<byte> input)
        {
            return _hashCore.Hash(input);
        }

        public static R Hash(byte[] input, int offset, int length)
        {
            return _hashCore.Hash(input, offset, length);
        }
    }
}
