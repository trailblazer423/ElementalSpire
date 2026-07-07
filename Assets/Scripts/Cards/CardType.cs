using System;

namespace ElementalSpire.Cards
{
    [Flags]
    public enum CardType
    {
        None = 0,
        Attack = 1,
        Skill = 2,
        Defense = 4,
        Power = 8,
        Trick = 16
    }
}
