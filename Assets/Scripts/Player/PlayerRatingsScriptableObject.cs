using UnityEngine;

namespace Sandbox.Player
{
    [CreateAssetMenu(fileName = "Player1", menuName = "Player Ratings")]
    public class PlayerRatingsScriptableObject : ScriptableObject
    {
        public float speed = 12f;
        public int power = 50;
        public float maxHealth = 100;
        public float startJumpGravityForce = 9.81f;
        public float addedNegativeForce = 0f;
        public float jumpDownGravityMultiplier = 1.0f;
        public float jumpY = 6f;
        public float airForce = 500f;


    }
}



