using Unity.Collections;

namespace DunefieldModel_DualMesh
{
using Unity.Collections;

    public struct NativeGrid
    {
        public NativeArray<float> data;
        private int width, height;

        public int Width => width;
        public int Height => height;

        public NativeGrid(int width, int height, Allocator allocator)
        {
            this.width = width;
            this.height = height;
            data = new NativeArray<float>(width * height, allocator, NativeArrayOptions.ClearMemory);
        }

        public float this[int x, int z]
        {
            get => data[WrappedIndex(x, z)];
            set => data[WrappedIndex(x, z)] = value;
        }

        private int WrappedIndex(int x, int z)
        {
            x = (x + width) % width;
            z = (z + height) % height;
            return x + z * width;
        }

        public NativeGrid Clone(Allocator allocator)
        {
            NativeGrid clone = new NativeGrid
            {
                width = this.width,
                height = this.height,
                data = new NativeArray<float>(this.data, allocator)
            };
            return clone;
        }

        public void CopyFrom(NativeGrid source)
        {
            if (source.width != width || source.height != height)
                throw new System.ArgumentException("Grid dimensions must match");

            NativeArray<float>.Copy(source.data, data);
        }

        public void Dispose()
        {
            if (data.IsCreated)
                data.Dispose();
        }
    }

}