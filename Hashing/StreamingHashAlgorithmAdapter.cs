using System;
using System.Runtime.InteropServices;

namespace NeoSmart.Hashing
{
    /// <summary>
    /// An adaptor that automatically implements <see cref="HashAlgorithm"/>
    /// for any type implementing <see cref="IStreamingHashAlgorithm{R}"/>.
    ///
    /// Unlike with <see cref="HashAlgorithmAdapter{T, R}"/>, this has a <see cref="CanReuseTransform"/> value
    /// of <c>false</c> and a <see cref="CanTransformMultipleBlocks"/> value of <c>true</c>.
    /// </summary>
    /// <typeparam name="T">The <see cref="IStreamingHashAlgorithm{R}"/> implementation</typeparam>
    /// <typeparam name="R">The result returned by the <see cref="IStreamingHashAlgorithm{R}"/> implementation</typeparam>
    public class StreamingHashAlgorithmAdapter<T,R> : System.Security.Cryptography.HashAlgorithm
        where T: IStreamingHashAlgorithm<R>, new()
        where R: struct
    {
        private IStreamingHashAlgorithm<R> _hasher;
        private byte[] _result;

        public StreamingHashAlgorithmAdapter()
        {
            _hasher = new T();
        }

        public override int HashSize => (int)(_hasher.HashLengthBits);

        public override void Initialize()
        {
            _hasher.Initialize();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            _hasher.Update(array.AsSpan(ibStart, cbSize));
        }

        protected override byte[] HashFinal()
        {
            var result = _hasher.Result;
            var resultBytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref result, 1));
            return _result = resultBytes.ToArray();
        }

        public override bool CanTransformMultipleBlocks => true;
        public override bool CanReuseTransform => false;
        public override byte[] Hash => _result;
    }
}