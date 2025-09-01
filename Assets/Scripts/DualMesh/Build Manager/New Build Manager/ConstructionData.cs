using System.Collections.Generic;
using ResourceSystem;

#region Enums
public enum ConstructionCategory
{
    Housing,
    Wall,
    BonusProvider
}

public enum Placer
{
    Single,
    Wall
}
#endregion

#region Classes
[Serializable]
public class BonusEffect
{
    public string type;
    public float value;
}

[Serializable]
public class BonusData
{
    public float radius;
    public List<BonusEffect> effects;
}

[Serializable]
public class ResourceAmount
{
    public Resource resource;
    public float amount;
}

[Serializable]
public class PrefabCost
{
    public string prefabName;
    public List<ResourceAmount> cost;
}
#endregion

[System.Serializable]
public class ConstructionData
{
    public string id;
    public string name;
    public string category;
    public Placer placerType;
    public List<string> prefabs;
    public List<PrefabCost> cost;
    public List<string> actions;
    public BonusData bonus;
}

[Serializable]
public class ConstructionDataList
{
    public ConstructionData[] constructions;
}

#region Json example
/*
Example for json file, where actions represents the dynamic behaviour when it is built

{
  "constructions": [
    {
      "id": "house_01",
      "name": "Casa Pequeña",
      "category": "Housing",
      "placerType": "Single",
      "prefabs": ["HousePrefab"],
      "prefabCosts": [
        {
          "prefabName": "HousePrefab",
          "cost": [
            { "resource": "Work", "amount": 50 },
            { "resource": "Sand", "amount": 20 }
          ]
        }
      ],
      "actions": ["PlaceOnGround", "ConsumeResources"]
    },
    {
      "id": "wall_01",
      "name": "Muralla de Madera",
      "category": "Wall",
      "placerType": "Wall",
      "prefabs": ["WallTowerPrefab", "WallSegmentPrefab"],
      "prefabCosts": [
        {
          "prefabName": "WallTowerPrefab",
          "cost": [
            { "resource": "Work", "amount": 100 }
          ]
        },
        {
          "prefabName": "WallSegmentPrefab",
          "cost": [
            { "resource": "Work", "amount": 50 }
          ]
        }
      ],
      "actions": ["PlaceOnGrid", "ConsumeResources", "ConnectToNeighbors"]
    },
    {
      "id": "shrine_01",
      "name": "Shrine of Growth",
      "category": "BonusProvider",
      "placerType": "Single",
      "prefabs": ["ShrinePrefab"],
      "prefabCosts": [
        {
          "prefabName": "ShrinePrefab",
          "cost": [
            { "resource": "Work", "amount": 200 },
            { "resource": "Sand", "amount": 150 }
          ]
        }
      ],
      "actions": ["PlaceOnGround", "ConsumeResources", "ApplyBonuses"],
      "bonus": {
        "radius": 5,
        "effects": [
          { "type": "ProductionBoost", "value": 1.2 },
          { "type": "DefenseBoost", "value": 0.15 }
        ]
      }
    }
  ]
}
*/
#endregion