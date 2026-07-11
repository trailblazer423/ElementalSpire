using System.Collections.Generic;

namespace ElementalSpire.Cards
{
    /// <summary>
    /// Converts between the compact card id stored in run data and a runtime card instance.
    /// An upgraded card is persisted as "card_id+" so older saves remain compatible.
    /// </summary>
    public static class CardInstanceCodec
    {
        private const char UpgradeSuffix = '+';

        public static bool TryDecode(string serializedCardId, out string cardId, out bool isUpgraded)
        {
            cardId = string.Empty;
            isUpgraded = false;

            if (string.IsNullOrWhiteSpace(serializedCardId))
                return false;

            string normalized = serializedCardId.Trim();
            if (normalized[normalized.Length - 1] == UpgradeSuffix)
            {
                isUpgraded = true;
                normalized = normalized.Substring(0, normalized.Length - 1).TrimEnd();
            }

            if (normalized.Length == 0)
                return false;

            cardId = normalized;
            return true;
        }

        public static CardInstance Decode(string serializedCardId)
        {
            return TryDecode(serializedCardId, out string cardId, out bool isUpgraded)
                ? new CardInstance(cardId, isUpgraded)
                : null;
        }

        public static string Encode(string cardId, bool isUpgraded)
        {
            if (string.IsNullOrWhiteSpace(cardId))
                return string.Empty;

            string normalized = cardId.Trim().TrimEnd(UpgradeSuffix);
            return isUpgraded ? normalized + UpgradeSuffix : normalized;
        }

        public static string Encode(CardInstance card)
        {
            return card == null ? string.Empty : Encode(card.cardId, card.isUpgraded);
        }

        public static List<CardInstance> DecodeMany(IEnumerable<string> serializedCardIds)
        {
            var cards = new List<CardInstance>();
            if (serializedCardIds == null)
                return cards;

            foreach (string serializedCardId in serializedCardIds)
            {
                CardInstance card = Decode(serializedCardId);
                if (card != null)
                    cards.Add(card);
            }

            return cards;
        }
    }
}
