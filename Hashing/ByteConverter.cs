using System;
using System.Collections.Generic;
using System.Text;

namespace NeoSmart.Hashing
{
    internal static class ByteConverter
    {
        public static byte[] ToBytes(Object v)
        {
            throw new NotImplementedException();
        }

        public static byte[] ToBytes(UInt32 v)
        {
            return BitConverter.GetBytes(v);
        }

        public static byte[] ToBytes(UInt64 v)
        {
            return BitConverter.GetBytes(v);
        }

        public static byte[] ToBytes(byte[] v)
        {
            return v;
        }
    }
}
