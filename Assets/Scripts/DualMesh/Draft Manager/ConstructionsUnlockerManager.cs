using System.Collections.Generic;
using Data;
using UnityEngine;

public static class ConstructionUnlockerManager
{
    static private HashSet<ConstructionType> unlockedConstructions = new();
    static public IReadOnlyCollection<ConstructionType> UnlockedConstructions => unlockedConstructions;

    public static void Awake()
    {
        unlockedConstructions.Add(ConstructionType.House);
        unlockedConstructions.Add(ConstructionType.Tower);
        unlockedConstructions.Add(ConstructionType.SegmentWall);
    }

    static public bool IsConstructionUnlocked(ConstructionType type)
    {
        return unlockedConstructions.Contains(type);
    }

    static public IEnumerable<ConstructionType> GetUnlockedConstructions()
    {
        return unlockedConstructions;
    }

    static public void UnlockConstruction(ConstructionType type)
    {
        if (!unlockedConstructions.Contains(type))
        {
            unlockedConstructions.Add(type);
            Debug.Log($"Construction {type} unlocked");
        }
        else
        {
            Debug.LogWarning($"Construction {type} is already unlocked");
        }
    }
}