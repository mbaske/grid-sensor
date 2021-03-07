using UnityEngine;

namespace MBaske.Sensors.Grid
{
    public enum MinDimensions : int
    {
        MATCH3 = 5,
        RESNET = 15,
        SIMPLE = 20,
        NATURE_CNN = 36
    }

    public struct GridShape
    {
        public int StackSize;
        public int Channels;
        public int Width;
        public int Height;

        public int Size => Channels * Width * Height;
        public int ChannelsPerStackLayer => Channels / StackSize;

        public GridShape(int stackSize, int channels, int width, int height)
        {
            StackSize = stackSize;
            Channels = channels * stackSize;
            Width = width;
            Height = height;
        }

        public GridShape(int stackSize, int channels, Vector2Int size)
            : this(stackSize, channels, size.x, size.y) { }

        public GridShape(int channels, int width, int height) 
            : this(1, channels, width, height) { }

        public GridShape(int channels, Vector2Int size) 
            : this(1, channels, size.x, size.y) { }

        
        public bool Validate(MinDimensions dim)
        {
            int d = (int)dim;
            return Width >= d && Height >= d && Channels > 0;
        }

        public int[] ToArray() => new int[3] { Height, Width, Channels };
        public override string ToString() => $"{Height} (height) x {Width} (width) x {Channels} (channels)";
    }
}