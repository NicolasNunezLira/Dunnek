using UnityEngine;

namespace ResourceSystem
{
    [System.Serializable]
    public class Resource
    {
        public string Name { get; private set; }
        public int Amount { get; private set; }
        public int Flux { get; private set; }

        public Resource(string name, int initialAmount = 0, int flux = 0)
        {
            Name = name;
            Amount = initialAmount;
            Flux = flux;
        }

        public void Add(int amount)
        {
            Amount += amount;
        }

        public bool TryConsume(int amount)
        {
            if (Amount >= amount)
            {
                Amount -= amount;
                return true;
            }
            return false;
        }

        public void UpdateFlux(int delta)
        {
            Flux += delta;
        }
    }
}
