using UnityEngine;
using Rewired;

namespace PixelCrushers.DialogueSystem.RewiredSupport
{

    /// <summary>
    /// This script tells the Dialogue System to use Rewired for
    /// GetButtonDown checks.
    /// </summary>
    [AddComponentMenu("Pixel Crushers/Dialogue System/Third Party/Rewired/Dialogue System Rewired")]
    public class DialogueSystemRewired : MonoBehaviour
    {

        /// <summary>
        /// Rewired player ID.
        /// </summary>
        [Tooltip("Rewired player ID.")]
        public int playerId = 0;

        private Player m_player;

        private void Start()
        {
            DialogueManager.GetInputButtonDown = RewiredGetButtonDown;
            m_player = ReInput.players.GetPlayer(playerId);
            if (m_player == null)
            {
                Debug.LogWarning("Dialogue System: Didn't find a Rewired player #" + playerId, this);
            }
            else
            {
                if (DialogueDebug.logInfo) Debug.Log("Dialogue System: Will read input from Rewired.", this);
            }
        }

        public bool RewiredGetButtonDown(string buttonName)
        {
            return (m_player != null) ? m_player.GetButtonDown(buttonName) : Input.GetButtonDown(buttonName);
        }

    }
}