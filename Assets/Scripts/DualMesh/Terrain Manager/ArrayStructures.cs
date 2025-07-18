using Unity.Mathematics;

namespace DunefieldModel_DualMesh
{
    using System.Collections.Generic;
    using System.Linq;
    using Data;
    using Unity.Collections;

    #region Native Grid
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
            /* How to use
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
    #endregion

    #region Frame Visual Changes
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
    #endregion

    #region ConstructionGrid
    public struct ConstructionGrid
    {
        public Dictionary<int2, Dictionary<int, ConstructionType>> data;

        private int width;
        private int length;

        public int Width => width;
        public int Length => length;

        public ConstructionGrid(int width, int length)
        {
            data = new Dictionary<int2, Dictionary<int, Data.ConstructionType>>();
            this.width = width;
            this.length = length;
        }

        public List<int> this[int x, int z]
        {
            get
            {
                var key = new int2(x, z);
                if (data.TryGetValue(key, out var innerDict))
                    return innerDict.Keys.ToList<int>();
                return new List<int>();
            }
        }

        public void AddConstruction(int x, int z, int id, Data.ConstructionType type)
        {
            var key = new int2(x, z);
            if (!data.ContainsKey(key))
                data[key] = new Dictionary<int, Data.ConstructionType>();

            data[key][id] = type;
        }

        public bool TryRemoveConstruction(int x, int z, int id)
        {
            int2 key = new int2(x, z);
            if (!data.ContainsKey(key))
            {
                return false;
            }

            return data[key].Remove(id);
        }

        public bool IsValid(int x, int z)
        {
            return x < 0 || z < 0 || x >= Width || z >= Length;
        }

        public bool TryGetType(int x, int z, ConstructionType constructionType, out List<int> ids)
        {
            ids = new List<int>();
            var key = new int2(x, z);

            if (data.TryGetValue(key, out var innerDict))
            {
                foreach (var kvp in innerDict)
                {
                    if (kvp.Value == constructionType)
                        ids.Add(kvp.Key);
                }

                return ids.Count > 0;
            }

            return false;
        }
    }
    #endregion
}