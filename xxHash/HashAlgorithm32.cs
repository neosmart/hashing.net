using System;
using System.Diagnostics;
using System.Security.Cryptography;

namespace NeoSmart.Hashing.XXHash
{
    public class HashAlgorithm32 : HashAlgorithm
    {
        private UInt32 hash;

        public override int HashSize => 32; //in bits

        public override void Initialize()
        {
        }

        protected override void HashCore(byte[] array, int ibStart, int cbSize)
        {
            var state = XXHash.CreateState32();
            bool result = XXHash.UpdateState32(state, array, ibStart, cbSize);
            Debug.Assert(result, "Internal xxhash failure!");
            hash = XXHash.DigestState32(state);
        }

        protected override byte[] HashFinal()
        {
            return BitConverter.GetBytes(hash);
        }
    }
}
