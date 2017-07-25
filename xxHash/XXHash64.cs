using System;
using System.Diagnostics;
using System.Text;
using XXCore = NeoSmart.Hashing.XXHash.Core.XXHash;

namespace NeoSmart.Hashing.XXHash
{
    public class XXHash64 : HashCore<XXHash64Core, XXHash64StreamingCore, UInt64>
    {
    }

    public struct XXHash64Core : IHashAlgorithm<UInt64>
    {
        public UInt32 HashLengthBits => 64;

        public UInt64 Hash(byte[] input, int offset, int length)
        {
            var state = XXCore.CreateState32();
            bool result = XXCore.UpdateState32(state, input, offset, length);
            Debug.Assert(result, "Internal xxHash library error!");
            return XXCore.DigestState32(state);
        }
    }

    public struct XXHash64StreamingCore : IStreamingHashAlgorithm<UInt64>
    {
        private XXCore.State64 _state;

        public void Initialize()
        {
            _state = XXCore.CreateState64();
        }

        public void Initialize(UInt64 seed)
        {
            _state = XXCore.CreateState64(seed);
        }

        public void Update(byte[] input, int offset, int length)
        {
            XXCore.UpdateState64(_state, input, offset, length);
        }

        public UInt64 Result => XXCore.DigestState64(_state);
    }
}
