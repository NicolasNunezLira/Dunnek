using UnityEngine;

namespace DraftSystem
{
    public enum DraftMode
    {
        UnlockOnly,
        BonusOnly,
        Mixed
    }
    
    public enum Rarity
    {
        Common,
        Uncommon,
        Rare,
        Legendary
    }

    public enum DraftState
    {
        Idle,
        Drafting,
        WaitingForPlayer,
        Finished
    }
}