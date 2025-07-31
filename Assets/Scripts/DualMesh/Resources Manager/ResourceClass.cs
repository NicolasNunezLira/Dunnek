using System.Collections.Generic;

namespace ResourceSystem
{
    [System.Serializable]
    public enum ResourceName
    { Work, Sand}

    [System.Serializable]
    public class Resource
    {
        public ResourceName Name { get; private set; }
        public float Amount { get; private set; }
        public float Rate { get; private set; }
        public Dictionary<int, float> Producers = new();

        public Resource(ResourceName name, float initialAmount = 0, float initialRate = 0)
        {
            Name = name;
            Amount = initialAmount;
            Rate = initialRate;
        }

        public void Add(float amount)
        {
            Amount += amount;
        }

        public void AddRate(float amount)
        {
            Rate += amount;
        }

        public bool TryConsume(float amount)
        {
            if (Amount >= amount)
            {
                Amount -= amount;
                return true;
            }
            return false;
        }
    }
}
