using System;
using System.Diagnostics;
using XXCore = NeoSmart.Hashing.XXHash.Core.XXHash;

namespace NeoSmart.Hashing.XXHash
{
    public class XXHash32 : HashCore<XXHash32Core, XXHash32StreamingCore, UInt32>
    {
    }

    public readonly struct XXHash32Core : IHashAlgorithm<UInt32>
    {
        public UInt32 HashLengthBits => 32;

        public UInt32 Hash(ReadOnlySpan<byte> input)
        {
            var state = XXCore.CreateState32();
            bool result = XXCore.UpdateState32(ref state, input);
            Debug.Assert(result, "Internal xxHash library error!");
            return XXCore.DigestState32(ref state);
        }
    }

    public class XXHash32StreamingCore : IStreamingHashAlgorithm<UInt32>
    {
        public UInt32 HashLengthBits => 32;

        private XXCore.State32 _state;

        public void Initialize()
        {
            _state = XXCore.CreateState32();
        }

        public void Initialize(UInt32 seed)
        {
            _state = XXCore.CreateState32(seed);
        }

        public void Update(ReadOnlySpan<byte> input)
        {
            XXCore.UpdateState32(ref _state, input);
        }

        public UInt32 Result => XXCore.DigestState32(ref _state);

        uint IHashAlgorithm<uint>.Hash(ReadOnlySpan<byte> input)
        {
            return XXCore.XXH32(input);
        }
    }
}
