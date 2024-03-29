#define EnableSimpleVersion
#undef  EnableSimpleVersion  // EnableSimpleVersion off
// The RawPointers implementation is not complete and it's not clear if there are any benefits to it (see https://godbolt.org/z/9zPaaaqsn)
#define RawPointers
#undef RawPointers

using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NeoSmart.Hashing.XXHash.Core
{
    public static class XXHash
    {
        /***************************************
        *  Constants
        ***************************************/
        const uint PRIME32_1 = 2654435761U,
                   PRIME32_2 = 2246822519U,
                   PRIME32_3 = 3266489917U,
                   PRIME32_4 =  668265263U,
                   PRIME32_5 =  374761393U;

        const ulong PRIME64_1 = 11400714785074694791UL,
                    PRIME64_2 = 14029467366897019727UL,
                    PRIME64_3 =  1609587929392839161UL,
                    PRIME64_4 =  9650029242287828579UL,
                    PRIME64_5 =  2870177450012600261UL;

        /*****************************
        *  Definitions
        *****************************/

        /*
        enum ErrorCode :
            Original C implementation definition:
              typedef enum { XXH_OK=0, XXH_ERROR } XXH_errorcode;
        */
        internal enum ErrorCode { XXH_OK = 0, XXH_ERROR }

        /******************************************
        *  Macros
        ******************************************/

        /*
        XXH_rotl32() :
            Rotates unsigned 32-bits integer "x" to the left by the number of bits specified in the "r" parameter.

            Original C implementation definition:
              #define XXH_rotl32(x,r) ((x << r) | (x >> (32 - r)))

        XXH_rotl64() :
            Rotates unsigned 64-bits integer "x" to the left by the number of bits specified in the "r" parameter.

            Original C implementation definition:
              #define XXH_rotl64(x,r) ((x << r) | (x >> (64 - r)))
        */
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if NETCOREAPP3_0_OR_GREATER
        // The compiler should recognize the bitshift pattern as an x86 rol, but just in case:
        static uint XXH_rotl32(uint x, int r) => System.Numerics.BitOperations.RotateLeft(x, r);
#else
        static uint  XXH_rotl32(uint x, int r) => ((x << r) | (x >> (32 - r)));
#endif
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#if NETCOREAPP3_0_OR_GREATER
        // The compiler should recognize the bitshift pattern as an x86 rol, but just in case:
        static ulong XXH_rotl64(ulong x, int r) => System.Numerics.BitOperations.RotateLeft(x, r);
#else
        static ulong XXH_rotl64(ulong x, int r) => ((x << r) | (x >> (64 - r)));
#endif

        /*****************************
        *  Simple Hash Functions
        *****************************/

        static public uint XXH32(ReadOnlySpan<byte> input)
        {
            return XXH32(input, 0U);
        }

        static public uint XXH32(byte[] input)
        {
            return XXH32(input, 0U);
        }

        static public uint XXH32(byte[] input, uint seed)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return XXH32(input, 0, input.Length, seed);
        }

        static public uint XXH32(Stream inputStream)
        {
            return XXH32(inputStream, 0U);
        }

        static public uint XXH32(Stream inputStream, uint seed)
        {
            State32 state = new State32();
            ResetState32(ref state, seed);
            UpdateState32(ref state, inputStream);
            return DigestState32(ref state);
        }

        static public ulong XXH64(ReadOnlySpan<byte> input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return XXH64(input);
        }
        static public ulong XXH64(byte[] input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return XXH64(input.AsSpan(), 0UL);
        }
        static public ulong XXH64(byte[] input, ulong seed)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return XXH64(input, 0, input.Length, seed);
        }
        static public ulong XXH64(Stream inputStream)
        {
            if (inputStream == null)
                throw new ArgumentNullException(nameof(inputStream));

            return XXH64(inputStream, 0U);
        }
        static public ulong XXH64(Stream inputStream, ulong seed)
        {
            State64 state = new State64();
            try
            {
                ResetState64(ref state, seed);
                UpdateState64(ref state, inputStream);
                return DigestState64(ref state);
            }
            catch
            {
                throw;
            }
        }

        static public uint XXH32(byte[] input, int offset, int length, uint seed)
        {
            if (offset < 0)
                ThrowArgumentNonNegativeNumber(nameof(offset)); ;
            if (length < 0)
                ThrowArgumentNonNegativeNumber(nameof(length));
            if (input.Length < (offset + length))
                ThrowArrayInvalidOffsetAndLength();
            if (input.Rank != 1)
                ThrowArrayMultiRank(nameof(input));
            if (input.GetLowerBound(0) != 0)
                ThrowArrayNonZeroLowerBound(nameof(input));

            return XXH32(input.AsSpan(offset, length), seed);
        }

        /*
        XXH32() :
            Calculate the 32-bits hash of sequence "length" bytes stored at memory address "input".
            The memory between offset & offset+length in "input" must be valid (allocated and read-accessible).
            "seed" can be used to alter the result predictably.

            Original C implementation definition:
              unsigned int       XXH32 (const void* input, size_t length, unsigned seed);

        XXH64() :
            Calculate the 64-bits hash of sequence "length" bytes stored at memory address "input".
            Faster on 64-bits systems. Slower on 32-bits systems.

            Original C implementation definition:
              unsigned long long XXH64 (const void* input, size_t length, unsigned long long seed);
        */
        static public uint  XXH32(ReadOnlySpan<byte> input, uint  seed)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

#if EnableSimpleVersion
            /* Simple version, good for code maintenance, but unfortunately slow for small inputs */
            State32 state = new State32();
            ResetState32(ref state, seed);
            UpdateState32(ref state, input);
            return DigestState32(ref state);
#else
            InputTextStream p = new InputTextStream(input);
            long bEnd = p.Position + input.Length;
            uint h32;

            if (input.Length >= 16)
            {
                long limit = bEnd - 16;
                uint v1 = seed + PRIME32_1 + PRIME32_2;
                uint v2 = seed + PRIME32_2;
                uint v3 = seed + 0;
                uint v4 = seed - PRIME32_1;

                do
                {
                    v1 += p.ReadUInt32() * PRIME32_2;
                    v1  = XXH_rotl32(v1, 13);
                    v1 *= PRIME32_1;

                    v2 += p.ReadUInt32() * PRIME32_2;
                    v2  = XXH_rotl32(v2, 13);
                    v2 *= PRIME32_1;

                    v3 += p.ReadUInt32() * PRIME32_2;
                    v3  = XXH_rotl32(v3, 13);
                    v3 *= PRIME32_1;

                    v4 += p.ReadUInt32() * PRIME32_2;
                    v4  = XXH_rotl32(v4, 13);
                    v4 *= PRIME32_1;
                }
                while (p.Position <= limit);

                h32 = XXH_rotl32(v1, 1) + XXH_rotl32(v2, 7) + XXH_rotl32(v3, 12) + XXH_rotl32(v4, 18);
            }
            else
            {
                h32 = seed + PRIME32_5;
            }

            h32 += (uint)input.Length;

            while (p.Position + 4 <= bEnd)
            {
                h32 += p.ReadUInt32() * PRIME32_3;
                h32 = XXH_rotl32(h32, 17) * PRIME32_4;
            }

            while (p.Position < bEnd)
            {
                h32 += p.ReadByte() * PRIME32_5;
                h32 = XXH_rotl32(h32, 11) * PRIME32_1;
            }

            h32 ^= h32 >> 15;
            h32 *= PRIME32_2;
            h32 ^= h32 >> 13;
            h32 *= PRIME32_3;
            h32 ^= h32 >> 16;

            return h32;
#endif
        }


        static public ulong XXH64(byte[] input, int offset, int length, ulong seed)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            if (input.Rank != 1)
                ThrowArrayMultiRank(nameof(input));
            if (input.GetLowerBound(0) != 0)
                ThrowArrayNonZeroLowerBound(nameof(input));
            if (offset < 0)
                ThrowArgumentNonNegativeNumber(nameof(offset));
            if (length < 0)
                ThrowArgumentNonNegativeNumber(nameof(length));
            if (input.Length < (offset + length))
                ThrowArrayInvalidOffsetAndLength();

            return XXH64(input.AsSpan(offset, length), seed);
        }

        static public ulong XXH64(ReadOnlySpan<byte> input, ulong seed)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

#if EnableSimpleVersion
            /* Simple version, good for code maintenance, but unfortunately slow for small inputs */
            State64 state = new State64();
            ResetState64(ref state, seed);
            UpdateState64(ref state, input);
            return DigestState64(ref state);
#else
            InputTextStream p = new InputTextStream(input);
            long bEnd = p.Position + input.Length;
            ulong h64;

            if (input.Length >= 32)
            {
                long limit = bEnd - 32;
                ulong v1 = seed + PRIME64_1 + PRIME64_2;
                ulong v2 = seed + PRIME64_2;
                ulong v3 = seed + 0;
                ulong v4 = seed - PRIME64_1;

                do
                {
                    v1 += p.ReadUInt64() * PRIME64_2;
                    v1  = XXH_rotl64(v1, 31);
                    v1 *= PRIME64_1;

                    v2 += p.ReadUInt64() * PRIME64_2;
                    v2  = XXH_rotl64(v2, 31);
                    v2 *= PRIME64_1;

                    v3 += p.ReadUInt64() * PRIME64_2;
                    v3  = XXH_rotl64(v3, 31);
                    v3 *= PRIME64_1;

                    v4 += p.ReadUInt64() * PRIME64_2;
                    v4  = XXH_rotl64(v4, 31);
                    v4 *= PRIME64_1;
                }
                while (p.Position <= limit);

                h64 = XXH_rotl64(v1, 1) + XXH_rotl64(v2, 7) + XXH_rotl64(v3, 12) + XXH_rotl64(v4, 18);

                v1 *= PRIME64_2;
                v1  = XXH_rotl64(v1, 31);
                v1 *= PRIME64_1;
                h64 ^= v1;
                h64  = h64 * PRIME64_1 + PRIME64_4;

                v2 *= PRIME64_2;
                v2  = XXH_rotl64(v2, 31);
                v2 *= PRIME64_1;
                h64 ^= v2;
                h64  = h64 * PRIME64_1 + PRIME64_4;

                v3 *= PRIME64_2;
                v3  = XXH_rotl64(v3, 31);
                v3 *= PRIME64_1;
                h64 ^= v3;
                h64  = h64 * PRIME64_1 + PRIME64_4;

                v4 *= PRIME64_2;
                v4  = XXH_rotl64(v4, 31);
                v4 *= PRIME64_1;
                h64 ^= v4;
                h64  = h64 * PRIME64_1 + PRIME64_4;
            }
            else
            {
                h64 = seed + PRIME64_5;
            }

            h64 += (ulong)input.Length;

            while (p.Position + 8 <= bEnd)
            {
                ulong k1 = p.ReadUInt64();
                k1 *= PRIME64_2;
                k1  = XXH_rotl64(k1, 31);
                k1 *= PRIME64_1;
                h64 ^= k1;
                h64  = XXH_rotl64(h64, 27) * PRIME64_1 + PRIME64_4;
            }

            if (p.Position + 4 <= bEnd)
            {
                h64 ^= (ulong)(p.ReadUInt32()) * PRIME64_1;
                h64  = XXH_rotl64(h64, 23) * PRIME64_2 + PRIME64_3;
            }

            while (p.Position < bEnd)
            {
                h64 ^= p.ReadByte() * PRIME64_5;
                h64  = XXH_rotl64(h64, 11) * PRIME64_1;
            }

            h64 ^= h64 >> 33;
            h64 *= PRIME64_2;
            h64 ^= h64 >> 29;
            h64 *= PRIME64_3;
            h64 ^= h64 >> 32;

            return h64;
#endif
        }

        /****************************************************
        *  Advanced Hash Functions
        ****************************************************/

        /*** Allocation ***/

        /* These structures allow static allocation of XXH states.
         * States must then be initialized using ResetStateXX() before first use.

         class State32 :
             Original C implementation definition:
               typedef struct { long long ll[ 6]; } XXH32_state_t;

         class State64 :
             Original C implementation definition:
               typedef struct { long long ll[11]; } XXH64_state_t;
        */
        [StructLayout(LayoutKind.Auto)]     // Original C implementation definition:
        public unsafe struct State32          // typedef struct
        {                                         // {
            internal ulong total_len;             //     U64 total_len;
            internal uint seed;                   //     U32 seed;
            internal uint v1;                     //     U32 v1;
            internal uint v2;                     //     U32 v2;
            internal uint v3;                     //     U32 v3;
            internal uint v4;                     //     U32 v4;
            internal fixed uint _mem32[4];         //     U32 mem32[4];   /* defined as U32 for alignment */
            internal Span<byte> mem32 { get { fixed (uint* ptr = _mem32) { return new Span<byte>(ptr, 16); } } }
            //internal byte[] _mem32;
            //internal Span<byte> mem32 => _mem32.AsSpan();
            internal uint memsize;                //     U32 memsize;
        }                                         // } XXH_istate32_t;

        [StructLayout(LayoutKind.Auto)]     // Original C implementation definition:
        public unsafe struct State64          // typedef struct
        {                                         // {
            internal ulong total_len;             //     U64 total_len;
            internal ulong seed;                  //     U64 seed;
            internal ulong v1;                    //     U64 v1;
            internal ulong v2;                    //     U64 v2;
            internal ulong v3;                    //     U64 v3;
            internal ulong v4;                    //     U64 v4;
            internal fixed ulong _mem64[4];        //     U64 mem64[4];   /* defined as U64 for alignment */
            internal Span<byte> mem64 { get { fixed (ulong* ptr = _mem64) { return new Span<byte>(ptr, 32); } } }
            //internal byte[] _mem64;
            //internal Span<byte> mem64 => _mem64.AsSpan();
            internal uint memsize;                //     U32 memsize;
        }                                         // } XXH_istate64_t;

        /* These functions create and initialize a XXH state object.

        CreateState32() :
            Original C implementation definition:
              XXH32_state_t* XXH32_createState(void);

        CreateState64() :
            Original C implementation definition:
              XXH64_state_t* XXH64_createState(void);
        */
        static public State32 CreateState32()
        {
            return CreateState32(0U);
        }
        static public State64 CreateState64()
        {
            return CreateState64(0U);
        }

        static public State32 CreateState32(uint  seed)
        {
            State32 value = new State32();
            ResetState32(ref value, seed);
            return value;
        }
        static public State64 CreateState64(ulong seed)
        {
            State64 value = new State64();
            ResetState64(ref value, seed);
            return value;
        }

        /*** Hash feed ***/

        /* These functions calculate the xxHash of an input provided in multiple smaller packets,
         * as opposed to an input provided as a single block.
         *
         * XXH state space must first be allocated.
         *
         * Start a new hash by initializing state with a seed, using ResetStateXX().
         *
         * Then, feed the hash state by calling UpdateStateXX() as many times as necessary.
         * Obviously, input must be valid, meaning allocated and read accessible.
         * The function returns an error code, with 0 meaning OK, and any other value meaning there is an error.
         *
         * Finally, you can produce a hash anytime, by using DigestStateXX().
         * This function returns the final XX-bits hash.
         * You can nonetheless continue feeding the hash state with more input,
         * and therefore get some new hashes, by calling again DigestStateXX().

        ResetState32(),
        UpdateState32(),
        DigestState32() :

            Original C implementation definition:
              XXH_errorcode XXH32_reset  (XXH32_state_t* statePtr, unsigned seed);
              XXH_errorcode XXH32_update (XXH32_state_t* statePtr, const void* input, size_t length);
              unsigned int  XXH32_digest (const XXH32_state_t* statePtr);

        ResetState64(),
        UpdateState64(),
        DigestState64() :

            Original C implementation definition:
              XXH_errorcode      XXH64_reset  (XXH64_state_t* statePtr, unsigned long long seed);
              XXH_errorcode      XXH64_update (XXH64_state_t* statePtr, const void* input, size_t length);
              unsigned long long XXH64_digest (const XXH64_state_t* statePtr);
        */
        static public void ResetState32(ref State32 state, uint seed)
        {
            InternalResetState32(ref state, seed);
        }
        static public bool UpdateState32(ref State32 state, ReadOnlySpan<byte> input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return (ErrorCode.XXH_OK == InternalUpdateState32(ref state, input));
        }
        static public bool UpdateState32(ref State32 state, ReadOnlySpan<byte> input, int offset, int length)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            if (offset < 0)
                ThrowArgumentNonNegativeNumber(nameof(offset));
            if (length < 0)
                ThrowArgumentNonNegativeNumber(nameof(length));
            if (input.Length < (offset + length))
                ThrowArrayInvalidOffsetAndLength();

            return (ErrorCode.XXH_OK == InternalUpdateState32(ref state, input.Slice(offset, length)));
        }
        static public bool UpdateState32(ref State32 state, Stream inputStream)
        {
            if (inputStream == null)
                throw new ArgumentNullException(nameof(inputStream));

            byte[] buffer = new byte[0x2000];
            int size;
            do
            {
                size = inputStream.Read(buffer, 0, 0x2000);
                if (size > 0)
                {
                    if (InternalUpdateState32(ref state, buffer.AsSpan(0, size)) != ErrorCode.XXH_OK)
                    {
                        return false;
                    }
                }
            } while (size > 0);
            return true;
        }
        static public uint DigestState32(ref State32 state)
        {
            return InternalDigestState32(ref state);
        }

        static public void ResetState64(ref State64 state, ulong seed)
        {
            InternalResetState64(ref state, seed);
        }
        static public bool UpdateState64(ref State64 state, byte[] input, int offset, int length)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));
            if (input.Rank != 1)
                ThrowArrayMultiRank(nameof(input));
            if (input.GetLowerBound(0) != 0)
                ThrowArrayNonZeroLowerBound(nameof(input));
            if (offset < 0)
                ThrowArgumentNonNegativeNumber(nameof(offset));
            if (length < 0)
                ThrowArgumentNonNegativeNumber(nameof(length));
            if (input.Length < (offset + length))
                ThrowArrayInvalidOffsetAndLength();

            return UpdateState64(ref state, input.AsSpan(offset, length));
        }
        static public bool UpdateState64(ref State64 state, ReadOnlySpan<byte> input)
        {
            if (input == null)
                throw new ArgumentNullException(nameof(input));

            return (ErrorCode.XXH_OK == InternalUpdateState64(ref state, input));
        }
        static public bool UpdateState64(ref State64 state, Stream inputStream)
        {
            if (inputStream == null)
                throw new ArgumentNullException(nameof(inputStream));

            byte[] buffer = new byte[0x2000];
            int size;
            do
            {
                size = inputStream.Read(buffer, 0, 0x2000);
                if (size > 0)
                {
                    if (InternalUpdateState64(ref state, buffer.AsSpan(0, size)) != ErrorCode.XXH_OK)
                    {
                        return false;
                    }
                }
            } while (size > 0);
            return true;
        }
        static public ulong DigestState64(ref State64 state)
        {
            return InternalDigestState64(ref state);
        }

        static internal void InternalResetState32(ref State32 state, uint seed)
        {
            state.seed = seed;
            state.v1 = seed + PRIME32_1 + PRIME32_2;
            state.v2 = seed + PRIME32_2;
            state.v3 = seed + 0;
            state.v4 = seed - PRIME32_1;
            state.total_len = 0;
            state.memsize = 0;
            //state.mem32.Fill(0);
            //state._mem32 = new byte[16];
        }

        static internal ErrorCode InternalUpdateState32(ref State32 state, ReadOnlySpan<byte> input)
        {
            InputTextStream p = new InputTextStream(input);
            long bEnd = p.Position + input.Length;

            state.total_len += (ulong)input.Length;

            if (state.memsize + input.Length < 16)   /* fill in tmp buffer */
            {
#if RawPointers
                unsafe
                {
                    fixed (byte *src = input)
                    fixed (uint *dst = state._mem32)
                    {
                        Buffer.MemoryCopy(src, ((byte*) dst) + state.memsize, 16, input.Length);
                    }
                }
#else
                input.CopyTo(state.mem32.Slice((int)state.memsize));
                //Array.Copy(input, offset, state.mem32, (int) state.memsize, length);
#endif
                state.memsize += (uint)input.Length;
                return ErrorCode.XXH_OK;
            }

            if (state.memsize > 0)   /* some data left from previous update */
            {
                input.Slice(0, (int)(16 - state.memsize)).CopyTo(state.mem32.Slice((int)state.memsize));
                //Array.Copy(input, offset, state.mem32, (int) state.memsize, (int) (16 - state.memsize));
                {
                    InputTextStream p32 = new InputTextStream(state.mem32);
                    state.v1 = XXH32_round(state.v1, p32.ReadUInt32());
                    state.v2 = XXH32_round(state.v2, p32.ReadUInt32());
                    state.v3 = XXH32_round(state.v3, p32.ReadUInt32());
                    state.v4 = XXH32_round(state.v4, p32.ReadUInt32());
                }
                p.Skip(16 - (int)state.memsize);
                state.memsize = 0;
            }

            if (p.Position <= bEnd - 16)
            {
                long limit = bEnd - 16;
                uint v1 = state.v1;
                uint v2 = state.v2;
                uint v3 = state.v3;
                uint v4 = state.v4;

                do
                {
                    v1 = XXH32_round(v1, p.ReadUInt32());
                    v2 = XXH32_round(v2, p.ReadUInt32());
                    v3 = XXH32_round(v3, p.ReadUInt32());
                    v4 = XXH32_round(v4, p.ReadUInt32());
                }
                while (p.Position <= limit);

                state.v1 = v1;
                state.v2 = v2;
                state.v3 = v3;
                state.v4 = v4;
            }

            if (p.Position < bEnd)
            {
                input.Slice(p.Position, (int)(bEnd - p.Position)).CopyTo(state.mem32);
                //Array.Copy(input, p.Position, state.mem32, 0, (int) (bEnd - p.Position));
                state.memsize = (uint)(bEnd - p.Position);
            }

            return ErrorCode.XXH_OK;
        }

        static internal uint InternalDigestState32(ref State32 state)
        {
            InputTextStream p = new InputTextStream(state.mem32);
            long bEnd = state.memsize;
            uint h32;

            if (state.total_len >= 16)
            {
                h32 = XXH_rotl32(state.v1, 1) + XXH_rotl32(state.v2, 7) + XXH_rotl32(state.v3, 12) + XXH_rotl32(state.v4, 18);
            }
            else
            {
                h32 = state.seed + PRIME32_5;
            }

            h32 += (uint)state.total_len;

            while (p.Position + 4 <= bEnd)
            {
                h32 += p.ReadUInt32() * PRIME32_3;
                h32 = XXH_rotl32(h32, 17) * PRIME32_4;
            }

            while (p.Position < bEnd)
            {
                h32 += p.ReadByte() * PRIME32_5;
                h32 = XXH_rotl32(h32, 11) * PRIME32_1;
            }

            h32 ^= h32 >> 15;
            h32 *= PRIME32_2;
            h32 ^= h32 >> 13;
            h32 *= PRIME32_3;
            h32 ^= h32 >> 16;

            return h32;
        }
        static private uint XXH32_round(uint seed, uint input)
        {
            seed += input * PRIME32_2;
            seed  = XXH_rotl32(seed, 13);
            seed *= PRIME32_1;
            return seed;
        }

        static internal void InternalResetState64(ref State64 state, ulong seed)
        {
            state.seed = seed;
            state.v1 = seed + PRIME64_1 + PRIME64_2;
            state.v2 = seed + PRIME64_2;
            state.v3 = seed + 0;
            state.v4 = seed - PRIME64_1;
            state.total_len = 0;
            state.memsize = 0;
            //state.mem64.Fill(0);
            //state._mem64 = new byte[32];
        }
        static internal ErrorCode InternalUpdateState64(ref State64 state, ReadOnlySpan<byte> input)
        {
            InputTextStream p = new InputTextStream(input);
            long bEnd = p.Position + input.Length;

            state.total_len += (ulong)input.Length;

            if (state.memsize + input.Length < 32)   /* fill in tmp buffer */
            {
                input.CopyTo(state.mem64.Slice((int)state.memsize));
                //Array.Copy(input, offset, state.mem64, (int) state.memsize, length);
                state.memsize += (uint)input.Length;
                return ErrorCode.XXH_OK;
            }

            if (state.memsize > 0)   /* tmp buffer is full */
            {
                input.Slice(0, (int)(32 - state.memsize)).CopyTo(state.mem64.Slice((int)state.memsize));
                //Array.Copy(input, offset, state.mem64, (int) state.memsize, (int) (32 - state.memsize));
                {
                    InputTextStream p64 = new InputTextStream(state.mem64);
                    state.v1 += p64.ReadUInt64() * PRIME64_2;
                    state.v1  = XXH_rotl64(state.v1, 31);
                    state.v1 *= PRIME64_1;

                    state.v2 += p64.ReadUInt64() * PRIME64_2;
                    state.v2  = XXH_rotl64(state.v2, 31);
                    state.v2 *= PRIME64_1;

                    state.v3 += p64.ReadUInt64() * PRIME64_2;
                    state.v3  = XXH_rotl64(state.v3, 31);
                    state.v3 *= PRIME64_1;

                    state.v4 += p64.ReadUInt64() * PRIME64_2;
                    state.v4  = XXH_rotl64(state.v4, 31);
                    state.v4 *= PRIME64_1;
                }
                p.Skip(32 - (int)state.memsize);
                state.memsize = 0;
            }

            if (p.Position + 32 <= bEnd)
            {
                long limit = bEnd - 32;
                ulong v1 = state.v1;
                ulong v2 = state.v2;
                ulong v3 = state.v3;
                ulong v4 = state.v4;

                do
                {
                    v1 = XXH64_round(v1, p.ReadUInt64());
                    v2 = XXH64_round(v2, p.ReadUInt64());
                    v3 = XXH64_round(v3, p.ReadUInt64());
                    v4 = XXH64_round(v4, p.ReadUInt64());
                }
                while (p.Position <= limit);

                state.v1 = v1;
                state.v2 = v2;
                state.v3 = v3;
                state.v4 = v4;
            }

            if (p.Position < bEnd)
            {
                input.Slice(p.Position, (int)(bEnd - p.Position)).CopyTo(state.mem64);
                //Array.Copy(input, p.Position, state.mem64, 0, (int) (bEnd - p.Position));
                state.memsize = (uint)(bEnd - p.Position);
            }

            return ErrorCode.XXH_OK;
        }
        static internal ulong InternalDigestState64(ref State64 state)
        {
            InputTextStream p = new InputTextStream(state.mem64);
            long bEnd = state.memsize;
            ulong h64;

            if (state.total_len >= 32)
            {
                ulong v1 = state.v1;
                ulong v2 = state.v2;
                ulong v3 = state.v3;
                ulong v4 = state.v4;

                h64 = XXH_rotl64(v1, 1) + XXH_rotl64(v2, 7) + XXH_rotl64(v3, 12) + XXH_rotl64(v4, 18);
                h64 = XXH64_mergeRound(h64, v1);
                h64 = XXH64_mergeRound(h64, v2);
                h64 = XXH64_mergeRound(h64, v3);
                h64 = XXH64_mergeRound(h64, v4);
            }
            else
            {
                h64 = state.v3 + PRIME64_5;
            }

            h64 += state.total_len;

            while (p.Position + 8 <= bEnd)
            {
                ulong k1 = XXH64_round(0, p.ReadUInt64());
                h64 ^= k1;
                h64  = XXH_rotl64(h64, 27) * PRIME64_1 + PRIME64_4;
            }

            if (p.Position + 4 <= bEnd)
            {
                h64 ^= (ulong)(p.ReadUInt32()) * PRIME64_1;
                h64  = XXH_rotl64(h64, 23) * PRIME64_2 + PRIME64_3;
            }

            while (p.Position < bEnd)
            {
                h64 ^= p.ReadByte() * PRIME64_5;
                h64  = XXH_rotl64(h64, 11) * PRIME64_1;
            }

            h64 ^= h64 >> 33;
            h64 *= PRIME64_2;
            h64 ^= h64 >> 29;
            h64 *= PRIME64_3;
            h64 ^= h64 >> 32;

            return h64;
        }
        static private ulong XXH64_round(ulong acc, ulong input)
        {
            acc += input * PRIME64_2;
            acc  = XXH_rotl64(acc, 31);
            acc *= PRIME64_1;
            return acc;
        }
        static private ulong XXH64_mergeRound(ulong acc, ulong val)
        {
            val  = XXH64_round(0, val);
            acc ^= val;
            acc  = acc * PRIME64_1 + PRIME64_4;
            return acc;
        }

#region Custom types and functions

        // The type provides reading data operations for the byte arrays.
        ref struct InputTextStream
        {
            private readonly ReadOnlySpan<byte> data;
            private int _position;

            // Gets the length in bytes of the stream.
            public int Length => data.Length;

            // Gets the position within the current stream.
            public int Position => _position;

            // Gets a value that indicates whether the current stream position is at the end of the stream.
            public bool EndOfStream => !(_position < this.data.Length);

            internal InputTextStream(ReadOnlySpan<byte> input)
            {
                this.data = input;
                _position = 0;
            }

            // Reads a 4-byte unsigned integer from the current stream and advances the position of the stream by
            // four bytes.
            public uint ReadUInt32()
            {
                if (EndOfStream)
                    throw new InvalidOperationException();

                uint value = MemoryMarshal.Read<UInt32>(data.Slice(_position));
                Skip(4);
                return value;
            }
            // Reads an 8-byte unsigned integer from the current stream and advances the position of the stream by
            // eight bytes.
            public ulong ReadUInt64()
            {
                if (EndOfStream)
                    throw new InvalidOperationException();

                ulong value = MemoryMarshal.Read<UInt64>(data.Slice(_position));
                Skip(8);
                return value;
            }
            // Reads the next byte from the current stream and advances the current position of the stream by one
            // byte.
            public byte ReadByte()
            {
                if (EndOfStream)
                    throw new InvalidOperationException();

                return this.data[_position++];
            }
            // Skip a number of bytes in the current stream.
            public bool Skip(int skipNumBytes)
            {
                if (skipNumBytes < 0)
                    throw new ArgumentException(nameof(skipNumBytes));

                if (!EndOfStream)
                {
                    if (_position + skipNumBytes > this.data.Length)
                    {
                        _position = this.data.Length;
                    }
                    else
                    {
                        _position += skipNumBytes;
                    }
                    return true;
                }
                return false;
            }
        }

        static void ThrowArgumentNonNegativeNumber(string paramName)
        {
            throw new ArgumentOutOfRangeException(paramName, "Specified argument must be a non-negative integer.");
        }
        static void ThrowArrayMultiRank(string paramName)
        {
            throw new ArgumentException("Multi dimension array is not supported on this operation.", paramName);
        }
        static void ThrowArrayNonZeroLowerBound(string paramName)
        {
            throw new ArgumentException("The lower bound of target array must be zero.", paramName);
        }
        static void ThrowArrayInvalidOffsetAndLength()
        {
            throw new ArgumentException(
                "Offset and length wereout of bounds for the array or count is greater than the number " +
                "of elements from offset to the end of the array.");
        }

#endregion
    }
}
