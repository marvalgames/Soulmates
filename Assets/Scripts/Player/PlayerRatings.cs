using Unity.Entities;
using UnityEngine;

namespace Sandbox.Player
{
    public class PlayerRatings : MonoBehaviour
    {

        public PlayerRatingsScriptableObject Ratings;
        public float meleeWeaponPower = 1;
        public float hitPower = 10;//punch kick
        public float speed = 12;
        public float combatSpeed = 6;




    }

    public class PlayerRatingsBaker : Baker<PlayerRatings>
    {
        public override void Bake(PlayerRatings authoring)
        {
            var e = GetEntity(authoring.gameObject, TransformUsageFlags.Dynamic);
            
            //Debug.Log("Player Ratings " + authoring.speed);

            AddComponent( e,
                    new RatingsComponent
                    {
                        tag = 1, maxHealth = authoring.Ratings.maxHealth, 
                        //speed = authoring.Ratings.speed,
                        //gameSpeed =  authoring.Ratings.speed,
                        speed = authoring.speed,
                        combatSpeed = authoring.combatSpeed,
                        gameSpeed =  authoring.speed,
                        gameCombatSpeed = authoring.combatSpeed,
                        gameWeaponPower = authoring.meleeWeaponPower,
                        WeaponPower = authoring.meleeWeaponPower,
                        hitPower = authoring.hitPower
                    })
                ;

        }
    }

}
