using System.Collections.Generic;

namespace ResourceSystem
{
    [System.Serializable]
    public enum Resource
    { Work, Sand}

    [System.Serializable]
    public class ResourceClass
    {
        public Resource Name { get; private set; }
        public float Amount { get; private set; }
        public float Rate { get; private set; }
        public Dictionary<int, float> producers = new();

        public ResourceClass(Resource name, float initialAmount = 0, float initialRate = 0)
        {
            Name = name;
            Amount = initialAmount;
            Rate = initialRate;
        }

        public void Add(float amount)
        {
            ChangeAmount(amount);
        }

        public void AddRate(float amount)
        {
            Rate += amount;
        }

        public void UpdateAmount()
        {
            ChangeAmount(Rate);
        }

        public bool TryConsume(float amount)
        {
            if (Amount >= amount)
            {
                ChangeAmount(amount);
                return true;
            }
            return false;
        }

        private void ChangeAmount(float value)
        {
            Amount += value;
        }
    }
}
