using System;
using System.Diagnostics;
using XXCore = NeoSmart.Hashing.XXHash.Core.XXHash;

namespace NeoSmart.Hashing.XXHash
{
    public class XXHash64 : HashCore<XXHash64Core, XXHash64StreamingCore, UInt64>
    {
    }

    public readonly struct XXHash64Core : IHashAlgorithm<UInt64>
    {
        public UInt32 HashLengthBits => 64;

        public UInt64 Hash(ReadOnlySpan<byte> input)
        {
            var state = XXCore.CreateState32();
            bool result = XXCore.UpdateState32(ref state, input);
            Debug.Assert(result, "Internal xxHash library error!");
            return XXCore.DigestState32(ref state);
        }
    }

    public class XXHash64StreamingCore : IStreamingHashAlgorithm<UInt64>
    {
        public UInt32 HashLengthBits => 64;

        private XXCore.State64 _state;

        public void Initialize()
        {
            _state = XXCore.CreateState64();
        }

        public void Initialize(UInt64 seed)
        {
            _state = XXCore.CreateState64(seed);
        }

        public void Update(ReadOnlySpan<byte> input)
        {
            XXCore.UpdateState64(ref _state, input);
        }

        public UInt64 Result => XXCore.DigestState64(ref _state);

        ulong IHashAlgorithm<ulong>.Hash(ReadOnlySpan<byte> input)
        {
            return XXCore.XXH64(input);
        }
    }
}
