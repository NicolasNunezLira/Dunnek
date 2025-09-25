using UnityEngine;
using System.Collections.Generic;
using ResourceSystem;

[CreateAssetMenu(fileName = "ResourceIconLibrary", menuName = "Config/ResourceIconLibrary")]
public class ResourceIconLibrary : ScriptableObject
{
    public static ResourceIconLibrary Instance;

    [System.Serializable]
    public class ResourceIcon
    {
        public Resource type;
        public Sprite icon;
    }

    public List<ResourceIcon> icons;
    private Dictionary<Resource, Sprite> iconDict;

    void Onable()
    {
        Instance = this;
        iconDict = new Dictionary<Resource, Sprite>();
        foreach (var res in icons)
            iconDict[res.type] = res.icon;
    }

    public Sprite GetIcon(Resource type)
    {
        return iconDict.TryGetValue(type, out var sprite) ? sprite : null;
    }
}