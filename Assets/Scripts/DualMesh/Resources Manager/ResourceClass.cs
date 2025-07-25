using UnityEngine;

namespace ResourceSystem
{
    [System.Serializable]
    public class Resource
    {
        public string Name { get; private set; }
        public float Amount { get; private set; }
        public float Flux { get; private set; }

        public Resource(string name, float initialAmount = 0, float flux = 0)
        {
            Name = name;
            Amount = initialAmount;
            Flux = flux;
        }

        public void Add(float amount)
        {
            Amount += amount;
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

        public void UpdateFlux(float delta)
        {
            Flux += delta;
        }
    }
}
