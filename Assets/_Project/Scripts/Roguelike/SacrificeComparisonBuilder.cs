using System.Collections.Generic;
using UnityEngine;

namespace ChezArthur.Roguelike
{
    /// <summary>
    /// Une ligne de l'encart : soit une ligne de stat (libellé + valeur), soit une ligne d'effet rédigée.
    /// </summary>
    public readonly struct ComparisonLine
    {
        public readonly bool IsEffectLine;
        public readonly string Text;       // effet : phrase complète ; stat : libellé de la stat
        public readonly float Magnitude;   // stat : valeur absolue ; effet : inutilisé
        public readonly bool IsCost;       // true = aspect négatif (downside)
        public readonly bool IsPercentage;

        private ComparisonLine(bool isEffect, string text, float magnitude, bool isCost, bool isPercentage)
        {
            IsEffectLine = isEffect; Text = text; Magnitude = magnitude; IsCost = isCost; IsPercentage = isPercentage;
        }

        public static ComparisonLine Stat(string label, float magnitude, bool isCost, bool isPercentage)
            => new ComparisonLine(false, label, Mathf.Abs(magnitude), isCost, isPercentage);

        public static ComparisonLine Effect(string text, bool isCost)
            => new ComparisonLine(true, text, 0f, isCost, false);
    }

    /// <summary>
    /// Construit les lignes de comparaison du sacrifice (valise perdue vs valise reçue).
    /// Remplit une liste fournie pour éviter les allocations répétées.
    /// </summary>
    public static class SacrificeComparisonBuilder
    {
        /// <summary> Lignes de la valise sacrifiée, depuis ses valeurs live. </summary>
        public static void BuildSacrificedLines(ValiseInstance sacrificed, List<ComparisonLine> output)
        {
            if (output == null) return;
            output.Clear();
            if (sacrificed == null || sacrificed.Data == null) return;
            ValiseData data = sacrificed.Data;

            if (data.ComparisonMode == ValiseComparisonMode.EffectLine)
            {
                output.Add(ComparisonLine.Effect(
                    RenderTemplate(data.EffectLineTemplate, sacrificed.GetTotalStatValue(), data.BaseIsPercentage), false));
                return;
            }

            output.Add(ComparisonLine.Stat(ValiseTypeUtility.GetStatLabel(data.BaseStatType),
                sacrificed.GetTotalStatValue(), false, data.BaseIsPercentage));
            if (data.HasSecondStat)
                output.Add(ComparisonLine.Stat(ValiseTypeUtility.GetStatLabel(data.SecondStatType),
                    sacrificed.GetTotalSecondStatValue(), false, data.SecondIsPercentage));
            if (data.HasDownside)
                output.Add(ComparisonLine.Stat(ValiseTypeUtility.GetStatLabel(data.DownsideStatType),
                    sacrificed.GetTotalDownsideValue(), true, data.DownsideIsPercentage));
        }

        /// <summary> Lignes de la valise reçue, depuis ses valeurs projetées au moment T. </summary>
        public static void BuildIncomingLines(ValiseData incoming, ValiseImprovementRarity rarity, List<ComparisonLine> output)
        {
            if (output == null) return;
            output.Clear();
            if (incoming == null) return;

            if (incoming.ComparisonMode == ValiseComparisonMode.EffectLine)
            {
                output.Add(ComparisonLine.Effect(
                    RenderTemplate(incoming.EffectLineTemplate, ValiseProjection.ProjectPrimaryValue(incoming, rarity), incoming.BaseIsPercentage), false));
                return;
            }

            output.Add(ComparisonLine.Stat(ValiseTypeUtility.GetStatLabel(incoming.BaseStatType),
                ValiseProjection.ProjectPrimaryValue(incoming, rarity), false, incoming.BaseIsPercentage));
            if (incoming.HasSecondStat)
                output.Add(ComparisonLine.Stat(ValiseTypeUtility.GetStatLabel(incoming.SecondStatType),
                    ValiseProjection.ProjectSecondValue(incoming, rarity), false, incoming.SecondIsPercentage));
            if (incoming.HasDownside)
                output.Add(ComparisonLine.Stat(ValiseTypeUtility.GetStatLabel(incoming.DownsideStatType),
                    ValiseProjection.ProjectDownsideValue(incoming, rarity), true, incoming.DownsideIsPercentage));
        }

        /// <summary> Remplace {value} par la valeur formatée (× 100 si pourcentage). </summary>
        private static string RenderTemplate(string template, float value, bool isPercentage)
        {
            if (string.IsNullOrEmpty(template)) return "";
            float display = isPercentage ? value * 100f : value;
            return template.Replace("{value}", display.ToString("0.#"));
        }

        /// <summary> Formate une magnitude : ×100 si pourcentage, signe fourni. Convention unique du sacrifice. </summary>
        public static string FormatMagnitude(float magnitude, bool isPercentage, string sign)
        {
            float display = isPercentage ? magnitude * 100f : magnitude;
            return $"{sign}{display.ToString("0.#")}{(isPercentage ? " %" : "")}";
        }

        /// <summary> Formate une ligne de comparaison en texte compact (libellé inclus pour les stats). </summary>
        public static string FormatLine(ComparisonLine line)
        {
            if (line.IsEffectLine) return line.Text;
            string sign = line.IsCost ? "−" : "+"; // « − » = U+2212, comme SacrificeUI
            return $"{FormatMagnitude(line.Magnitude, line.IsPercentage, sign)} {line.Text}";
        }

        /// <summary> Résumé live d'une valise pour l'affichage en liste (slot de sacrifice). </summary>
        public static string BuildSacrificedSummary(ValiseInstance instance, List<ComparisonLine> buffer)
        {
            if (buffer == null) return "";
            BuildSacrificedLines(instance, buffer);
            if (buffer.Count == 0) return "";
            if (buffer.Count == 1) return FormatLine(buffer[0]);

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < buffer.Count; i++)
            {
                if (i > 0) sb.Append(" · ");
                sb.Append(FormatLine(buffer[i]));
            }
            return sb.ToString();
        }
    }
}
