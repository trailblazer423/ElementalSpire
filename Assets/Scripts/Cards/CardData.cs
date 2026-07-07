namespace ElementalSpire.Cards
{
    public sealed class CardData
    {
        public string cardId;
        public int cardNumber;
        public ElementType elementType;
        public string cardName;
        public string rarity;
        public CardType cardType;
        public int cost;
        public int waterCost;
        public string description;
        public bool exhaust;
        public bool chooseElement;

        public bool HasCardType(CardType targetCardType)
        {
            return (cardType & targetCardType) == targetCardType;
        }
    }
}
