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
        public bool starter;
        public bool startEligible;
        public string rewardStage;
        public string[] keywords;

        public bool HasCardType(CardType targetCardType)
        {
            return (cardType & targetCardType) == targetCardType;
        }

        public bool HasKeyword(string keyword)
        {
            if (keywords == null || string.IsNullOrEmpty(keyword))
            {
                return false;
            }

            for (int i = 0; i < keywords.Length; i++)
            {
                if (keywords[i] == keyword)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
