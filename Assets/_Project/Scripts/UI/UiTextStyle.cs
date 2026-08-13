using TMPro;
using UnityEngine;

namespace ChezArthur.UI
{
    /// <summary>
    /// Styles de texte nommés RUI1 — mapping vers tokens existants (aucune valeur modifiée).
    /// </summary>
    public enum UiTextStyle
    {
        Display = 0,
        H1 = 1,
        H2 = 2,
        Body = 3,
        Caption = 4,
        Chip = 5
    }

    /// <summary>
    /// Applique taille / graisse / couleur depuis UiTheme + UiTypography.
    /// Pas de TMP StyleSheet.
    /// </summary>
    public static class UiTextStyleUtil
    {
        public static void Apply(TextMeshProUGUI tmp, UiTextStyle style)
        {
            if (tmp == null)
                return;

            switch (style)
            {
                case UiTextStyle.Display:
                    tmp.fontSize = UiTypography.Display;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.color = UiTheme.TextPrimary;
                    break;
                case UiTextStyle.H1:
                    tmp.fontSize = UiTypography.Title;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.color = UiTheme.TextPrimary;
                    break;
                case UiTextStyle.H2:
                    tmp.fontSize = UiTheme.FontHeader;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.color = UiTheme.TextPrimary;
                    break;
                case UiTextStyle.Body:
                    tmp.fontSize = UiTypography.Body;
                    tmp.fontStyle = FontStyles.Normal;
                    tmp.color = UiTheme.TextSecondary;
                    break;
                case UiTextStyle.Caption:
                    tmp.fontSize = UiTypography.Caption;
                    tmp.fontStyle = FontStyles.Normal;
                    tmp.color = UiTheme.TextMuted;
                    break;
                case UiTextStyle.Chip:
                    tmp.fontSize = UiTheme.FontLabel;
                    tmp.fontStyle = FontStyles.Bold;
                    tmp.color = UiTheme.TextPrimary;
                    tmp.characterSpacing = 4f;
                    break;
                default:
                    tmp.fontSize = UiTheme.FontBody;
                    tmp.fontStyle = FontStyles.Normal;
                    tmp.color = UiTheme.TextPrimary;
                    break;
            }
        }
    }
}
