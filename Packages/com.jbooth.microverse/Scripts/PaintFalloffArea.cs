using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace JBooth.MicroVerseCore
{
    public class PaintFalloffArea : Stamp, IModifier
    {
        [Tooltip("Paint Falloff Area object to use for falloff")]
        public FalloffFilter.PaintMask paintMask = new FalloffFilter.PaintMask();
        [Tooltip("If true, the falloff will be clamped to the bounds of the stamp, if false, the falloff will be unaffected outside of the stamp")]
        public bool clampOutsideOfBounds = true;

        public void Dispose()
        {
 
        }

        public void Initialize()
        {

        }

        public override Bounds GetBounds()
        {
            return TerrainUtil.GetBounds(transform);
        }


        void OnDrawGizmosSelected()
        {
            if (MicroVerse.instance != null)
            {
                Gizmos.color = Color.white;
                Gizmos.matrix = transform.localToWorldMatrix;
                Gizmos.DrawWireCube(new Vector3(0, 0.5f, 0), Vector3.one);
            }
        }

    }
}
