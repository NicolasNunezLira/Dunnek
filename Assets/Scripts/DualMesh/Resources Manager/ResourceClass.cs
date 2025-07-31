using UnityEngine;

namespace ResourceSystem
{
    [System.Serializable]
    public enum ResourceName
    { Workers, WorkForce, Sand}

    [System.Serializable]
    public class Resource
    {
        public ResourceName Name { get; private set; }
        public float Available { get; private set; }
        public float Cummulated { get; private set; }

        public Resource(ResourceName name, float initialAmount = 0)
        {
            Name = name;
            Cummulated = initialAmount;
            Available = Cummulated;
        }

        public void Add(float amount)
        {
            Cummulated += amount;
            Available += amount;
        }

        public bool TryConsume(float amount)
        {
            if (Available >= amount)
            {
                Available -= amount;
                return true;
            }
            return false;
        }
    }
}
