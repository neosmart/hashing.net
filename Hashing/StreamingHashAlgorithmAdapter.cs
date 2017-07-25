using System;

namespace NeoSmart.Hashing
{
    public class StreamingHashAlgorithmAdapter<R> : System.Security.Cryptography.HashAlgorithm
    {
        private IStreamingHashAlgorithm<R> _hasher;
        private readonly UInt32 _hashSize;
        private byte[] _hash;
        
        internal StreamingHashAlgorithmAdapter(IStreamingHashAlgorithm<R> hasher, UInt32 hashSize)
        {
            _hasher = hasher;
            _hashSize = hashSize;
        }

        public override int HashSize => (int)(_hashSize);

        public override void Initialize()
        {
            _hasher.Initialize();
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            _hasher.Update(array, ibStart, cbSize);
        }

        protected override byte[] HashFinal()
        {
            _hash = ByteConverter.ToBytes(_hasher.Result);
            return _hash;
        }

#if !NETSTANDARD1_3
        public override bool CanTransformMultipleBlocks => true;
        public override bool CanReuseTransform => true;
        public override byte[] Hash => _hash;
#endif
    }
}