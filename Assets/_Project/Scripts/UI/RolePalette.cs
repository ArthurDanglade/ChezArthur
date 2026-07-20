using UnityEngine;
using ChezArthur.Characters;

namespace ChezArthur.UI
{
    /// <summary>
    /// Source unique d'ACCÈS aux accents de rôle ; les valeurs vivent dans UiTheme (Role*).
    /// </summary>
    public static class RolePalette
    {
        /// <summary> Couleur associée à un rôle (neutre si inconnu). </summary>
        public static Color GetColor(CharacterRole role) => role switch
        {
            CharacterRole.Attacker => UiTheme.RoleAttacker,
            CharacterRole.Defender => UiTheme.RoleDefender,
            CharacterRole.Support => UiTheme.RoleSupport,
            _ => UiTheme.RoleNeutral
        };
    }
}
