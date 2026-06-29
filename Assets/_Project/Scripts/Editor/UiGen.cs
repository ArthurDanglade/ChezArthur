#if UNITY_EDITOR
using UnityEngine;
using TMPro;
using UnityEditor;
using ChezArthur.UI; // UiTheme

namespace ChezArthur.EditorTools
{
    /// <summary>
    /// Helpers partagés par les générateurs UI : chargement par nom du sprite de carte,
    /// de la police TMP (via UiTheme.FontDefault) et wiring de champ sérialisé.
    /// Centralise ce qui était dupliqué dans chaque générateur.
    /// </summary>
    public static class UiGen
    {
        /// <summary> Charge un sprite par son nom exact, ou null. </summary>
        public static Sprite LoadSprite(string spriteName)
        {
            if (string.IsNullOrEmpty(spriteName)) return null;
            foreach (var g in AssetDatabase.FindAssets($"{spriteName} t:Sprite"))
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(g));
                if (s != null && s.name == spriteName) return s;
            }
            return null;
        }

        /// <summary> Sprite de carte du thème, avec repli sur l'UISprite intégré. </summary>
        public static Sprite Card => LoadSprite(UiTheme.SpriteCard)
            ?? AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        /// <summary> Knob intégré (placeholder d'icône). </summary>
        public static Sprite Knob => AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");

        /// <summary> Police TMP du thème (UiTheme.FontDefault), ou null = police TMP par défaut. </summary>
        public static TMP_FontAsset LoadFont()
        {
            if (string.IsNullOrEmpty(UiTheme.FontDefault)) return null;
            foreach (var g in AssetDatabase.FindAssets($"{UiTheme.FontDefault} t:TMP_FontAsset"))
            {
                var f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(g));
                if (f != null) return f;
            }
            return null;
        }

        /// <summary> Branche un champ sérialisé sur une valeur (avec avertissement si introuvable). </summary>
        public static void Wire(SerializedObject so, string field, Object value)
        {
            var p = so.FindProperty(field);
            if (p != null) p.objectReferenceValue = value;
            else Debug.LogWarning($"[UiGen] Champ '{field}' introuvable sur {so.targetObject.GetType().Name}.");
        }
    }
}
#endif
