using Unity.Cinemachine;
using Unity.Entities;
using UnityEngine;

namespace Collisions
{
    public class Impulse : MonoBehaviour
    {
        [Header("Player Only")] [Tooltip("Player Hurt")]
        public CinemachineImpulseSource impulseSourceHitReceived;

        [Tooltip("Player Hits Enemy")] public CinemachineImpulseSource impulseSourceHitLanded;

        
    }
}
