using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using NeoSmart.Hashing.XXHash;

namespace NeoSmart.Hashing.Tests
{
    [TestClass]
    public class xxHash64Tests
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

                Assert.AreEqual(Trusted64(buffer), XXHash64.Hash(buffer));
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
                var state = new XXHash64();

                //generate a hash update with a different size each time
                var offset = 0;
                var bytesRemaining = length;
                for (int i = 0; bytesRemaining > 0; i = ((i + 1) % updateLength.Count))
                {
                    var cycleCount = Math.Min(updateLength[i], bytesRemaining);
                    state.Update(buffer, offset, cycleCount);
                    offset += cycleCount;
                    bytesRemaining -= cycleCount;
                }

                //finalize the hash and compare
                Assert.AreEqual(Trusted64(buffer), state.Result);
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
                var state = new XXHash64();

                var offset = 0;
                var bytesRemaining = length;
                while (bytesRemaining > 0)
                {
                    var cycleCount = Math.Min(streamLength, bytesRemaining);
                    state.Update(buffer, offset, cycleCount);
                    offset += cycleCount;
                    bytesRemaining -= cycleCount;
                }

                //finalize the hash and compare
                Assert.AreEqual(Trusted64(buffer), state.Result);
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
                Assert.AreEqual(Trusted64(buffer, seed), XXHash64.Hash(seed, buffer));
            }
        }

        private static ulong Trusted64(byte[] buffer, uint seed = 0)
        {
            var xxHash = System.Data.HashFunction.xxHash.xxHashFactory.Instance.Create(new System.Data.HashFunction.xxHash.xxHashConfig()
            {
                HashSizeInBits = 64,
                Seed = seed
            });
            return BitConverter.ToUInt64(xxHash.ComputeHash(buffer).Hash, 0);
        }
    }
}