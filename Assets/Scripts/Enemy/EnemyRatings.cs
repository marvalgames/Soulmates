using UnityEngine;
using Unity.Entities;



public class EnemyRatings : MonoBehaviour
{
    [SerializeField] bool randomize;

    public EnemyRatingsScriptableObject Ratings;
    //EntityManager manager;
    //Entity e;

  

    class EnemyRatingsBaker : Baker<EnemyRatings>
    {

        public override void Bake(EnemyRatings authoring)
        {
            var speed = authoring.Ratings.speed;
            var shootRangeDistance = authoring.Ratings.shootRangeDistance;
            var chaseRangeDistance = authoring.Ratings.chaseRange;
            var combatRangeDistance = authoring.Ratings.combatRangeDistance;
            var hitPower = authoring.Ratings.hitPower;

            if (authoring.randomize == true)
            {
                var multiplier = .5f;
            
                speed = Random.Range(speed, speed * (2 - multiplier));

                
                shootRangeDistance = Random.Range(shootRangeDistance * multiplier,
                    shootRangeDistance * (2 - multiplier));
                chaseRangeDistance = Random.Range(chaseRangeDistance * multiplier,
                    chaseRangeDistance * (2 - multiplier));
                

                //manager.SetComponentData<RatingsComponent>(e, ratings);
            }
            
            var e = GetEntity(TransformUsageFlags.Dynamic);

            
            AddComponent(e,
            
                new RatingsComponent
                {
                    tag = 2, maxHealth = authoring.Ratings.maxHealth,
                    speed = speed,
                    shootRangeDistance = shootRangeDistance,
                    chaseRangeDistance = chaseRangeDistance,
                    combatRangeDistance = combatRangeDistance,
                    hitPower = hitPower
                });
            
            //Debug.Log("RATINGS BAKED " + speed);

            
            //authoring.e = e;
            
         
        }
    }
}
    
  




