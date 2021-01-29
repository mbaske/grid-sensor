using UnityEngine;

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
        public int StackSize;
        public int Channels;
        public int Width;
        public int Height;

        public int Size => Channels * Width * Height;
        public int ChannelsPerStackLayer => Channels / StackSize;

        public GridObservationShape(int stackSize, int channels, int width, int height)
        {
            StackSize = stackSize;
            Channels = channels * stackSize;
            Width = width;
            Height = height;
        }

        public GridObservationShape(int stackSize, int channels, Vector2Int size)
            : this(stackSize, channels, size.x, size.y) { }

        public GridObservationShape(int channels, int width, int height) 
            : this(1, channels, width, height) { }

        public GridObservationShape(int channels, Vector2Int size) 
            : this(1, channels, size.x, size.y) { }

        
        public bool Validate(EncoderType type)
        {
            int s = (int)type;
            return Width >= s && Height >= s && Channels > 0;
        }

        public int[] ToArray() => new int[3] { Height, Width, Channels };
        public override string ToString() => $"GridObservationShape H{Height} x W{Width} x C{Channels}";
    }
}