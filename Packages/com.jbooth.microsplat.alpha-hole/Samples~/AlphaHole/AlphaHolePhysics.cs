using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AlphaHolePhysics : MonoBehaviour {

   public class ExampleClass : MonoBehaviour 
   {
      public LayerMask disableLayer;
      public Terrain terrain;
      public Collider terrainCollider;
      public string tagName = "Player";

      void Start()
      {
         if (terrain != null)
         {
            terrainCollider = terrain.GetComponent<Collider>();
         }
      }

      void OnTriggerEnter(Collider other) 
      {
         if (other.CompareTag(tagName))
         {
            Physics.IgnoreCollision(other, terrainCollider, true);
         }
      }

      void OnTriggerExit(Collider other) 
      {
         if (other.CompareTag(tagName))
         {
            Physics.IgnoreCollision(other, terrainCollider, false);
         }
      }
   }
}
