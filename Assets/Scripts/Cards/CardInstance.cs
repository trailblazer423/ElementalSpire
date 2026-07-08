namespace ElementalSpire.Cards
{
    [System.Serializable]
    public sealed class CardInstance
    {
        public string cardId;
        public bool isUpgraded;

        public CardInstance()
        {
        }

        public CardInstance(string cardId, bool isUpgraded = false)
        {
            this.cardId = cardId;
            this.isUpgraded = isUpgraded;
        }

        public CardData GetCardData()
        {
            return CardDeckLibrary.GetCardById(cardId);
        }

        public int GetEnergyCost()
        {
            return GetCardData().GetEnergyCost(isUpgraded);
        }

        public int GetWaterCost()
        {
            return GetCardData().GetWaterCost(isUpgraded);
        }
    }
}
