using UnityEngine;

namespace ElementalSpire.Cards
{
    /// <summary>
    /// 单张卡牌可复用的视觉配置。资源放入 Resources/CardAnimationProfiles 后，
    /// 可以通过 cardId 自动匹配，不需要修改卡牌战斗数值。
    /// </summary>
    [CreateAssetMenu(fileName = "CardAnimationProfile", menuName = "Elemental Spire/Card Animation Profile")]
    public sealed class CardAnimationProfile : ScriptableObject
    {
        [Header("卡牌匹配")]
        public string cardId;
        public Sprite cardImage;

        [Header("悬停动画")]
        [Min(0f)] public float hoverHeight = 55f;
        [Min(1f)] public float hoverScale = 1.18f;
        [Min(0.01f)] public float hoverDuration = 0.16f;
        [Min(0f)] public float floatingAmplitude = 4f;
        [Min(0f)] public float floatingSpeed = 2.4f;
        public Color hoverGlowColor = new Color(1f, 0.82f, 0.24f, 0.95f);

        [Header("选中动画")]
        public Color selectedGlowColor = new Color(0.2f, 0.72f, 1f, 1f);
        [Min(0f)] public float selectedShake = 2.2f;
        [Min(0f)] public float haloRotationSpeed = 70f;

        [Header("打出动画")]
        [Min(0.05f)] public float playDuration = 0.42f;
        [Min(1f)] public float playScale = 1.32f;
        public float playRotation = 420f;
        [Range(0f, 1f)] public float arcHeight = 0.24f;

        [Header("粒子与一次性特效")]
        public GameObject hoverVfxPrefab;
        public GameObject playVfxPrefab;
        public GameObject attackVfxPrefab;
        [Min(0.01f)] public float particleSize = 0.28f;
        [Min(0.1f)] public float particleSpeed = 7f;

        private static CardAnimationProfile[] _cachedProfiles;

        public static CardAnimationProfile Resolve(CardData cardData)
        {
            if (cardData == null)
                return null;
            if (cardData.animationProfile != null)
                return cardData.animationProfile;

            if (_cachedProfiles == null)
                _cachedProfiles = Resources.LoadAll<CardAnimationProfile>("CardAnimationProfiles");

            foreach (CardAnimationProfile profile in _cachedProfiles)
            {
                if (profile != null && profile.cardId == cardData.cardId)
                    return profile;
            }
            return null;
        }
    }
}
