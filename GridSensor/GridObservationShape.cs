
namespace MLGridSensor
{
    public enum EncoderType : int
    {
        MATCH3 = 5,
        RESNET = 15,
        SIMPLE = 20,
        NATURE_CNN = 36
    }

    public struct GridObservationShape
    {
        public int Channels;
        public int Width;
        public int Height;

        public int Size => Channels * Width * Height;

        public GridObservationShape(int channels, int width, int height)
        {
            Channels = channels;
            Width = width;
            Height = height;
        }

        public bool Validate(EncoderType type)
        {
            int s = (int)type;
            return Width >= s && Height >= s && Channels > 0;
        }

        public int[] ToArray() => new int[3] { Height, Width, Channels };
        public override string ToString() => $"GridObservationShape H{Height} x W{Width} x C{Channels}";
    }
}