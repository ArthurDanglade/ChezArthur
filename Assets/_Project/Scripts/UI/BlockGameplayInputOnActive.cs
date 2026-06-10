using UnityEngine;
using ChezArthur.Core;

namespace ChezArthur.UI
{
    /// <summary>
    /// À poser sur le root d'un panneau bloquant (sélection bonus, sacrifice, menu pause, Gare).
    /// Acquiert le verrou d'input gameplay tant que le GameObject est actif, le libère sinon.
    /// Branché sur OnEnable/OnDisable : se ré-équilibre tout seul, même en cas de destruction.
    /// </summary>
    public class BlockGameplayInputOnActive : MonoBehaviour
    {
        private bool _hasLock;

        private void OnEnable()
        {
            if (_hasLock) return;
            GameplayInputLock.Acquire();
            _hasLock = true;
        }

        private void OnDisable()
        {
            if (!_hasLock) return;
            GameplayInputLock.Release();
            _hasLock = false;
        }
    }
}
