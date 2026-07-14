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
        public string upgradeDescription;
        public int upgradedCost;
        public int upgradedWaterCost;
        public bool hasUpgrade;
        public bool exhaust;
        public bool chooseElement;
        public bool starter;
        public bool startEligible;
        public string rewardStage;
        public string[] keywords;

        // 可选视觉资源。未配置时由动画系统使用元素默认特效。
        public UnityEngine.Sprite cardImage;
        public CardAnimationProfile animationProfile;
        public UnityEngine.GameObject hoverVfxPrefab;
        public UnityEngine.GameObject playVfxPrefab;
        public UnityEngine.GameObject attackVfxPrefab;

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

        public int GetEnergyCost(bool isUpgraded)
        {
            return isUpgraded && upgradedCost >= 0 ? upgradedCost : cost;
        }

        public int GetWaterCost(bool isUpgraded)
        {
            return isUpgraded && upgradedWaterCost >= 0 ? upgradedWaterCost : waterCost;
        }
    }
}

