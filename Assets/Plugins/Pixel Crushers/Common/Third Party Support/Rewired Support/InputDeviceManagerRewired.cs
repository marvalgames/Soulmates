// Copyright (c) Pixel Crushers. All rights reserved.

using UnityEngine;
using Rewired;

namespace PixelCrushers.RewiredSupport
{

    /// <summary>
    /// This script tells the Input Device Manager to use Rewired for
    /// GetButtonDown and GetAxis checks.
    /// </summary>
    [AddComponentMenu("Pixel Crushers/Third Party/Rewired Support/Input Device Manager Rewired")]
    public class InputDeviceManagerRewired : MonoBehaviour
    {

        /// <summary>
        /// Rewired player ID.
        /// </summary>
        [Tooltip("Rewired player ID.")]
        public int playerId = 0;

        private Rewired.Player m_player;

        private void Start()
        {
            if (InputDeviceManager.instance == null) Debug.LogWarning("The scene is missing an Input Device Manager. Can't set up InputDeviceManagerRewired.", this);
            InputDeviceManager.instance.GetButtonDown = RewiredGetButtonDown;
            InputDeviceManager.instance.GetInputAxis = RewiredGetAxis;
            m_player = ReInput.players.GetPlayer(playerId);
            if (m_player == null) Debug.LogWarning("Didn't find a Rewired player #" + playerId, this);
        }

        public bool RewiredGetButtonDown(string buttonName)
        {
            return (m_player != null) ? (m_player.GetButtonDown(buttonName)) ? true : m_player.GetNegativeButtonDown(buttonName) : Input.GetButtonDown(buttonName);
        }

        public float RewiredGetAxis(string axisName)
        {
            return (m_player != null) ? m_player.GetAxis(axisName) : Input.GetAxis(axisName);
        }

    }
}
