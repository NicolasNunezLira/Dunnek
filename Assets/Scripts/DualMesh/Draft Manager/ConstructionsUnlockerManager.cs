using System.Collections.Generic;
using UnityEngine;
using ConstructionSystem;

public static class ConstructionUnlockerManager
{
    // Ahora usamos string en lugar de ConstructionType
    static private HashSet<string> unlockedConstructions = new();
    static public IReadOnlyCollection<string> UnlockedConstructions => unlockedConstructions;

    // Inicialización con construcciones desbloqueadas por defecto
    public static void Awake()
    {
        unlockedConstructions.Add("houseSand");
        unlockedConstructions.Add("initialTower");
        unlockedConstructions.Add("wallSand");
    }

    // Verifica si una construcción está desbloqueada
    static public bool IsConstructionUnlocked(string codeName)
    {
        return unlockedConstructions.Contains(codeName);
    }

    // Devuelve todas las construcciones desbloqueadas
    static public IEnumerable<string> GetUnlockedConstructions()
    {
        return unlockedConstructions;
    }

    // Desbloquea una construcción por su codeName
    static public void UnlockConstruction(string codeName)
    {
        if (!unlockedConstructions.Contains(codeName))
        {
            unlockedConstructions.Add(codeName);
            Debug.Log($"Construction {codeName} unlocked");
        }
        else
        {
            Debug.LogWarning($"Construction {codeName} is already unlocked");
        }
    }
}
