using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using NeoSmart.Hashing.XXHash;

namespace xxHash.Tests
{
    [TestClass]
    public class xxHash32Tests
    {
        private List<int> BufferLengths
        {
            get
            {
                var bufferLengths = new List<int>();
                for (var i = 0; i < 128; ++i)
                {
                    bufferLengths.Add(i);
                }

                for (var i = 4096; i != 0; i = (i >> 1))
                {
                    bufferLengths.Add(i);
                }

                return bufferLengths;
            }
        }

        [TestMethod]
        public void BasicHashTest()
        {
            var rng = new Random();
            foreach (var length in BufferLengths)
            {
                //fill buffer with random test data
                var buffer = new byte[length];
                rng.NextBytes(buffer);

                Assert.AreEqual(Trusted32(buffer), LocalHashInOneGo32(buffer));
            }
        }

        /// <summary>
        /// Tests the streaming API with a differently-sized incremental update in the same test.
        /// </summary>
        [TestMethod]
        public void MixedStreamingTest()
        {
            var updateLength = new List<int>();
            for (int i = 0; i <= 33; ++i)
            {
                updateLength.Add(i);
            }

            var rng = new Random();
            foreach (var length in BufferLengths)
            {
                //fill buffer with random test data
                var buffer = new byte[length];
                rng.NextBytes(buffer);

                //Initialize the hash
                var state = XXHash.CreateState32(0);

                //generate a hash update with a different size each time
                var offset = 0;
                var bytesRemaining = length;
                for (int i = 0; bytesRemaining > 0; i = ((i + 1) % updateLength.Count))
                {
                    var cycleCount = Math.Min(updateLength[i], bytesRemaining);
                    XXHash.UpdateState32(state, buffer, offset, cycleCount);
                    offset += cycleCount;
                    bytesRemaining -= cycleCount;
                }

                //finalize the hash and compare
                Assert.AreEqual(Trusted32(buffer), XXHash.DigestState32(state));
            }
        }

        /// <summary>
        /// Tests the streaming API with different update chunk sizes
        /// </summary>
        [TestMethod]
        public void StreamingTest()
        {
            var updateLength = new List<int>();
            for (int i = 1; i <= 34; ++i)
            {
                updateLength.Add(i);
            }

            var index = 0;
            var rng = new Random();
            foreach (var length in BufferLengths)
            {
                //pick a stream length
                var streamLength = updateLength[index];
                index = (index + 1) % updateLength.Count;

                //fill buffer with random test data
                var buffer = new byte[length];
                rng.NextBytes(buffer);

                //Initialize the hash
                var state = XXHash.CreateState32(0);

                var offset = 0;
                var bytesRemaining = length;
                while (bytesRemaining > 0)
                {
                    var cycleCount = Math.Min(streamLength, bytesRemaining);
                    XXHash.UpdateState32(state, buffer, offset, cycleCount);
                    offset += cycleCount;
                    bytesRemaining -= cycleCount;
                }

                //finalize the hash and compare
                Assert.AreEqual(Trusted32(buffer), XXHash.DigestState32(state));
            }
        }

        [TestMethod]
        public void SeedTest()
        {
            var rng = new Random();
            foreach (var length in BufferLengths)
            {
                //fill buffer with random test data
                var buffer = new byte[length];
                rng.NextBytes(buffer);

                var seed = (uint)rng.Next();
                Assert.AreEqual(Trusted32(buffer, seed), LocalHashInOneGo32(buffer, seed));
            }
        }

        private static uint LocalHashInOneGo32(byte[] buffer, uint seed = 0)
        {
            var state = XXHash.CreateState32(seed);
            XXHash.UpdateState32(state, buffer);
            return XXHash.DigestState32(state);
        }

        private static uint Trusted32(byte[] buffer, uint seed = 0)
        {
            var xxHash = new System.Data.HashFunction.xxHash();
            xxHash.InitVal = seed;
            return BitConverter.ToUInt32(xxHash.ComputeHash(buffer), 0);
        }
    }
}
