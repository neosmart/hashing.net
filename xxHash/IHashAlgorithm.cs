namespace NeoSmart.Hashing
{
    public interface IHashAlgorithm<R>
    {
        R Hash(byte[] input, int offset, int length);
    }
}
