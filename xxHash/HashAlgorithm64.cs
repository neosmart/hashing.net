using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace NeoSmart.Hashing.XXHash
{
    public class HashAlgorithm64 : HashAlgorithm
    {
        private UInt64 hash;

        public override int HashSize => 64; //in bits

        public override void Initialize()
        {
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            var state = XXHash.CreateState64();
            bool result = XXHash.UpdateState64(state, array, ibStart, cbSize);
            Debug.Assert(result, "Internal xxhash failure!");
            hash = XXHash.DigestState64(state);
        }

        protected override byte[] HashFinal()
        {
            return BitConverter.GetBytes(hash);
        }
    }
}
