using UnityEngine;

namespace Sandbox.Player
{
    [CreateAssetMenu(fileName = "PowerUp", menuName = "Create Power Up")]
    public class PowerUpScriptableObject : ScriptableObject
    {
        public float speed = 3f;
        public int power = 10;
        public float maxHealth = 100;
        public float powerTimeOn = 3;
        public float powerMultiplier = 1.5f;
        public int powerUseIncrease = 3;//for dash
        public bool powerSlowDown = false;
        public PickupType pickupType = PickupType.None;
        public string powerItemDescription;
        public string powerItemLongDescription;

        [Header ("Stats")]
        public string statDescription1;
        public float statRating1;
        public string statLongDescription1;
        public string statDescription2;
        public float statRating2;
        public string statLongDescription2;
        public string statDescription3;
        public float statRating3;
        public string statLongDescription3;



    }
}



