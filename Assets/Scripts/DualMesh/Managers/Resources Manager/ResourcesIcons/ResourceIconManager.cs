using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utils;

namespace ResourceSystem
{
    public class ResoureIconManager : Singleton<ResoureIconManager>
    {
        [Header("Icon Scriptable Objects")]
        [SerializeField] private List<ResourceSprite> iconLibrary;

        private Dictionary<Resource, Sprite> iconDict;

        protected override void Awake()
        {
            base.Awake();

            iconDict = iconLibrary.ToDictionary(
                entry => entry.resource,
                entry => entry.sprite
            );
        }

        public Sprite GetIcon(Resource resource)
        {
            return iconDict.TryGetValue(resource, out Sprite sprite) ? sprite : null;
        }
    }

    [System.Serializable]
    public struct ResourceSprite
    {
        public Resource resource;
        public Sprite sprite;
    }
}