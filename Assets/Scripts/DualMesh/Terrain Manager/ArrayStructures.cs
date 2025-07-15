using Unity.Collections;
using Unity.Mathematics;

namespace DunefieldModel_DualMesh
{
    using Unity.Collections;
    using Unity.VisualScripting;
    using UnityEngine;

    public struct NativeGrid
    {
        public NativeArray<float> data;
        private int width, height, visualWidth, visualHeight;
        public int Width => width;
        public int VisualWidth => visualWidth;
        public int Height => height;
        public int VisualHeight => visualHeight;

        public NativeArray<int> visualIndex;

        public NativeGrid(int width, int height, int visualWidth, int visualHeight, Allocator allocator)
        {
            this.width = width; // Dof x axis
            this.height = height; // Dof z axis
            this.visualWidth = visualWidth; // Dof visual x axis
            this.visualHeight = visualHeight; // Dof visual z axis
            data = new NativeArray<float>(width * height, allocator, NativeArrayOptions.ClearMemory);
            visualIndex = new NativeArray<int>(visualWidth * visualHeight, Allocator.Persistent); ;
            GenerateVisualIndex();
        }

        public float this[int x, int z]
        {
            get => data[WrappedIndex(x, z)];
            set => data[WrappedIndex(x, z)] = value;
        }

        private int WrappedIndex(int x, int z)
        {
            x = ((x % width) + width) % width;
            z = ((z % height) + height) % height;

            return x + z * width;
        }

        public (int, int) IndexToPosition(int i)
        {
            return (i % width, i / width);
        }

        public NativeGrid Clone(Allocator allocator)
        {
            NativeGrid clone = new NativeGrid
            {
                width = this.width,
                height = this.height,
                visualWidth = this.visualWidth,
                visualHeight = this.visualHeight,
                data = new NativeArray<float>(this.data, allocator),
                visualIndex = new NativeArray<int>(this.visualIndex, allocator)
            };
            return clone;
        }

        public void CopyFrom(NativeGrid source)
        {
            if (source.width != width || source.height != height)
                throw new System.ArgumentException("Grid dimensions must match");

            NativeArray<float>.Copy(source.data, data);
        }

        private void GenerateVisualIndex()
        {
            int i = 0;
            for (int z = 0; z < Height; z++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (
                        x >= 0 && x < VisualWidth &&
                        z >= 0 && z < VisualHeight
                    )
                    {
                        visualIndex[i] = WrappedIndex(x, z);
                        i++;
                    }
                }
            }
        }

        public void ForEachVisibleCell(System.Action<int, int, float> action)
        {
            /*
            grid.ForEachVisibleCell((x, z, value) =>
            {
                int meshIndex = x + z * grid.VisualWidth;
                vertices[meshIndex].y = value;
            });
            */
            for (int vz = 0; vz < visualHeight; vz++)
            {
                for (int vx = 0; vx < visualWidth; vx++)
                {
                    int index = visualIndex[vx + vz * visualWidth];
                    action(vx, vz, data[index]);
                }
            }
        }

        public void Dispose()
        {
            if (visualIndex.IsCreated)
                visualIndex.Dispose();
            if (data.IsCreated)
                data.Dispose();
        }
    }

    public struct FrameVisualChanges
    {
        public NativeHashSet<int2> changes;
        private int xDOF, zDOF;

        public FrameVisualChanges(int xDOF, int zDOF)
        {
            this.xDOF = xDOF;
            this.zDOF = zDOF;
            changes = new NativeHashSet<int2>(xDOF * zDOF, Allocator.Persistent);
        }

        public void AddChanges(int x, int z)
        {
            if (x >= 0 && x < xDOF && z >= 0 && z < zDOF)
            {
                changes.Add(new int2(x, z));
            }
        }

        public void ClearChanges()
        {
            if (changes.IsCreated)
                changes.Clear();
        }
        public void Dispose()
        {
            if (changes.IsCreated)
                changes.Dispose();
        }
    }

}