namespace ElementalSpire.Cards
{
    [System.Serializable]
    public sealed class CardInstance
    {
        public string cardId;
        public bool isUpgraded;
        public int energyCostModifier;
        public int waterCostModifier;

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
            CardData data = GetCardData();
            if (data == null) return 0;

            int finalCost = data.GetEnergyCost(isUpgraded) + energyCostModifier;
            return finalCost > 0 ? finalCost : 0;
        }

        public int GetWaterCost()
        {
            CardData data = GetCardData();
            if (data == null) return 0;

            int finalCost = data.GetWaterCost(isUpgraded) + waterCostModifier;
            return finalCost > 0 ? finalCost : 0;
        }

        public void ClearTemporaryCostModifiers()
        {
            energyCostModifier = 0;
            waterCostModifier = 0;
        }
    }
}
