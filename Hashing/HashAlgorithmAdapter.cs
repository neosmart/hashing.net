using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace NeoSmart.Hashing
{
    /// <summary>
    /// An adaptor that automatically implements <see cref="HashAlgorithm"/>
    /// for any type implementing <see cref="IHashAlgorithm{R}"/>.
    ///
    /// Unlike with <see cref="StreamingHashAlgorithmAdapter{T, R}"/>, this has a <see cref="CanReuseTransform"/> value
    /// of <c>true</c> and a <see cref="CanTransformMultipleBlocks"/> value of <c>false</c>.
    /// </summary>
    /// <typeparam name="T">The <see cref="IHashAlgorithm{R}"/> implementation</typeparam>
    /// <typeparam name="R">The result returned by the <see cref="IHashAlgorithm{R}"/> implementation</typeparam>
    public class HashAlgorithmAdapter<T,R> : HashAlgorithm
        where T: IHashAlgorithm<R>, new()
        where R: struct
    {
        private byte[] _result;
        private readonly T _hasher;

        internal HashAlgorithmAdapter()
        {
        }

        public override int HashSize => (int)_hasher.HashLengthBits;

        public override void Initialize()
        {
        }

        protected override void HashCore(ReadOnlySpan<byte> source)
        {
            var result = _hasher.Hash(source);
            var resultBytes = MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref result, 1));
            _result = resultBytes.ToArray();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            HashCore(array.AsSpan(ibStart, cbSize));
        }

        protected override byte[] HashFinal()
        {
            return _result;
        }

        public override bool CanTransformMultipleBlocks => false;
        public override bool CanReuseTransform => true;
        public override byte[] Hash => _result;
    }
}
