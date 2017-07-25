using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace NeoSmart.Hashing
{
    public class HashAlgorithmAdapter<T,R> : HashAlgorithm
        where T: IHashAlgorithm<R>
    {
        byte[] _result;

        internal HashAlgorithmAdapter()
        {
        }

        public override int HashSize => (int)(HashSingleton<T,R>.HashLengthBits);

        public override void Initialize()
        {
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            _result = ByteConverter.ToBytes(_result);
        }

        protected override byte[] HashFinal()
        {
            return _result;
        }

#if !NETSTANDARD1_3
        public override bool CanTransformMultipleBlocks => false;
        public override bool CanReuseTransform => true;
        public override byte[] Hash => _result;
#endif
    }
}
