using System;
using System.Collections.Generic;
using System.Text;

namespace NeoSmart.Hashing
{
    public abstract class HashCore<T, I, R>
        where T : IHashAlgorithm<R>
        where I : IStreamingHashAlgorithm<R>
    {
        //Core hash function
        public static R Hash(byte[] input, int offset, int length)
        {
            return HashSingleton<T, R>.Hash(input, offset, length);
        }

        //Hash helpers
        public static R Hash(byte[] input)
        {
            return Hash(input, 0, input.Length);
        }

        public static R Hash(string input)
        {
            return Hash(Encoding.UTF8.GetBytes(input));
        }

        //Seeded hash helper methods
        public static R Hash(R seed, byte[] input)
        {
            return Hash(seed, input, 0, input.Length);
        }

        public static R Hash(R seed, string input)
        {
            var bytes = Encoding.UTF8.GetBytes(input);
            return Hash(seed, bytes, 0, bytes.Length);
        }

        public static R Hash(R seed, byte[] input, int offset, int length)
        {
            var hasher = default(I);
            hasher.Initialize(seed);
            hasher.Update(input, offset, length);
            return hasher.Result;
        }

        //stateful incremental hash methods
        private IStreamingHashAlgorithm<R> _incrementalHash;

        public HashCore()
        {
            _incrementalHash = default(I);
            _incrementalHash.Initialize();
        }

        public HashCore(R seed)
        {
            _incrementalHash = default(I);
            _incrementalHash.Initialize(seed);
        }

        public void Update(byte[] input)
        {
            Update(input, 0, input.Length);
        }

        public void Update(byte[] input, int offset, int length)
        {
            _incrementalHash.Update(input, offset, length);
        }

        public R Result => _incrementalHash.Result;

        public static HashAlgorithmAdapter<T, R> HashAlgorithmAdapter => new HashAlgorithmAdapter<T, R>();
#if !NETSTANDARD1_3
        public static StreamingHashAlgorithmAdapter<R> StreamingHashAlgorithmAdapter => new StreamingHashAlgorithmAdapter<R>(default(I), HashSingleton<T, R>.HashLengthBits);
#endif
    }
}
