using System;

namespace NeoSmart.Hashing
{
    /// <summary>
    /// Used by hashing algorithms that do not support a streaming/incremental interface.
    /// Throws a NotImplementedException() when used.
    /// </summary>
    public struct NonStreamingHash<R> : IStreamingHashAlgorithm<R>
    {
        public R Result => throw new NotImplementedException();

        public void Initialize()
        {
            throw new NotImplementedException();
        }

        public void Initialize(R seed)
        {
            throw new NotImplementedException();
        }

        public void Update(byte[] input)
        {
            throw new NotImplementedException();
        }

        public void Update(byte[] input, int offset, int length)
        {
            throw new NotImplementedException();
        }
    }
}
