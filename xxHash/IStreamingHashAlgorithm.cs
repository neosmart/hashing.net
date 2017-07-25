namespace NeoSmart.Hashing
{
    public interface IStreamingHashAlgorithm<R>
    {
        void Initialize();
        void Initialize(R seed);
        void Update(byte[] input, int offset, int length);
        R Result { get; }
    }
}
