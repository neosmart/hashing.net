using System;

namespace NeoSmart.Hashing
{
    public abstract class HashCore<T, I, R>
        where T : IHashAlgorithm<R>, new()
        where I : IStreamingHashAlgorithm<R>, new()
        where R : struct
    {
        public static uint HashLengthBits { get; } = HashSingleton<T, R>.HashLengthBits;

        // Core hash function
        public static R Hash(ReadOnlySpan<byte> input)
        {
            return HashSingleton<T, R>.Hash(input);
        }

        public static R Hash(byte[] input, int offset, int length)
        {
            return Hash(input.AsSpan(offset, length));
        }

        // Seeded hash helper methods
        public static R Hash(R seed, ReadOnlySpan<byte> input)
        {
            var hasher = new I();
            hasher.Initialize(seed);
            hasher.Update(input);
            return hasher.Result;
        }

        // Stateful incremental hash methods
        private IStreamingHashAlgorithm<R> _incrementalHash;

        public HashCore()
        {
            _incrementalHash = new I();
            _incrementalHash.Initialize();
        }

        public HashCore(R seed)
        {
            _incrementalHash = new I();
            _incrementalHash.Initialize(seed);
        }

        public void Update(ReadOnlySpan<byte> input)
        {
            Update(input);
        }

        public void Update(byte[] input, int offset, int length)
        {
            _incrementalHash.Update(input.AsSpan(offset, length));
        }

        public R Result => _incrementalHash.Result;

        public static HashAlgorithmAdapter<T, R> HashAlgorithmAdapter => new HashAlgorithmAdapter<T, R>();
        public static StreamingHashAlgorithmAdapter<I, R> StreamingHashAlgorithmAdapter => new StreamingHashAlgorithmAdapter<I, R>();
    }
}
