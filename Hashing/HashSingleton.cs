using System;
using System.Text;

namespace NeoSmart.Hashing
{
    public static class HashSingleton<T,R>
        where T: IHashAlgorithm<R>
    {
        private static T _hashCore;

        public static UInt32 HashLengthBits => _hashCore.HashLengthBits;

        static HashSingleton()
        {
            _hashCore = default(T);
        }

        public static R Hash(byte[] input)
        {
            return Hash(input, 0, input.Length);
        }

        public static R Hash(string input)
        {
            return Hash(Encoding.UTF8.GetBytes(input));
        }

        public static R Hash(byte[] input, int offset, int length)
        {
            return _hashCore.Hash(input, offset, length);
        }
    }
}
