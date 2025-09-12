using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using DunefieldModel_DualMesh;

namespace ConstructionSystem
{
    [System.Serializable]
    public class WallGroup
    {
        public int GroupID { get; private set; }
        public List<ConstructionInstance> Parts { get; private set; }

        public WallGroup(int groupId)
        {
            GroupID = groupId;
            Parts = new List<ConstructionInstance>();
        }

        public void AddPart(ConstructionInstance part)
        {
            Parts.Add(part);
        }

        public bool IsBuried(NativeGrid sandElev, ConstructionGrid constructionGrid, FrameVisualChanges sandChanges)
        {
            bool allBuried = true;
            foreach (var part in Parts)
            {
                var (buried, _, _, _) = part.IsBuried(sandElev, constructionGrid, sandChanges);
                if (!buried) allBuried = false;
            }
            return allBuried;
        }

        public List<int2> ErodeAll(NativeGrid sandElev, ConstructionGrid constructionGrid, FrameVisualChanges changes)
        {
            var allCells = new List<int2>();
            foreach (var part in Parts)
            {
                allCells.AddRange(part.ErodeBuild(sandElev, constructionGrid, changes));
            }
            return allCells;
        }

        public void Destroy()
        {
            foreach (var part in Parts)
            {
                GameObject.Destroy(part.Obj);
            }
            Parts.Clear();
        }
    }  
}