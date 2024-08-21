using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;


namespace JBooth.MicroVerseCore
{
    public class VegetationUtilities
    {
        public static int FindDetailIndex(Terrain terrain, DetailPrototypeSerializable prototype)
        {
            int detailIndex = -1;
            var terrainDetails = terrain.terrainData.detailPrototypes;
            for (int i = 0; i < terrainDetails.Length; ++i)
            {
                var tp = terrainDetails[i];
                if (prototype.IsEqualToDetail(tp))
                {
                    detailIndex = i;
                }
            }
            return detailIndex;
        }

        public static int FindTreeIndex(Terrain terrain, TreePrototypeSerializable prototype)
        {
            int index = -1;
            var terrainDetails = terrain.terrainData.treePrototypes;
            for (int i = 0; i < terrainDetails.Length; ++i)
            {
                var tp = terrainDetails[i];
                if (prototype.IsEqualToTree(tp))
                {
                    index = i;
                }
            }
            return index;
        }



    }
}
