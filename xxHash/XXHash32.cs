using System;
using System.Diagnostics;
using System.Text;
using XXCore = NeoSmart.Hashing.XXHash.Core.XXHash;

namespace NeoSmart.Hashing.XXHash
{
    public class XXHash32 : HashCore<XXHash32Core, XXHash32StreamingCore, UInt32>
    {
    }

    public struct XXHash32Core : IHashAlgorithm<UInt32>
    {
        public UInt32 Hash(byte[] input, int offset, int length)
        {
            var state = XXCore.CreateState32();
            bool result = XXCore.UpdateState32(state, input, offset, length);
            Debug.Assert(result, "Internal xxHash library error!");
            return XXCore.DigestState32(state);
        }
    }

    public struct XXHash32StreamingCore : IStreamingHashAlgorithm<UInt32>
    {
        private XXCore.State32 _state;

        public void Initialize()
        {
            _state = XXCore.CreateState32();
        }

        public void Initialize(UInt32 seed)
        {
            _state = XXCore.CreateState32(seed);
        }

        public void Update(byte[] input, int offset, int length)
        {
            XXCore.UpdateState32(_state, input, offset, length);
        }

        public UInt32 Result => XXCore.DigestState32(_state);
    }
}
