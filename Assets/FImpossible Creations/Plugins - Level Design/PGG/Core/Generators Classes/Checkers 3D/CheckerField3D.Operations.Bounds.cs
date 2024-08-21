using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Generating.Checker
{
    public partial class CheckerField3D
    {

        public static Bounds ScaleBoundsForSweep(Bounds sweepBounds, Vector3 direction, int scaleUnits, bool shrinkBack = false, bool skrinkFurther = false)
        {
            if (sweepBounds.size == Vector3.zero) return sweepBounds;
            if (direction == Vector3.zero) return sweepBounds;
            if (scaleUnits < 1) return sweepBounds;

            Vector3 originalPos = sweepBounds.center;
            Vector3 originalSize = sweepBounds.size;
            Vector3 encapsulatePos = sweepBounds.center;
            Vector3 oSize = sweepBounds.size;
            Vector3 oMax = sweepBounds.max;
            Vector3 oMin = sweepBounds.min;

            if (direction.x != 0f)
            {
                if (direction.x > 0f) encapsulatePos.x = sweepBounds.max.x + scaleUnits;
                else encapsulatePos.x = sweepBounds.min.x - scaleUnits;

                sweepBounds.Encapsulate(encapsulatePos);
            }

            if (direction.y != 0f)
            {
                encapsulatePos = sweepBounds.center;

                if (direction.y > 0f) encapsulatePos.y = sweepBounds.max.y + scaleUnits;
                else encapsulatePos.y = sweepBounds.min.y - scaleUnits;
                sweepBounds.Encapsulate(encapsulatePos);
            }

            if (direction.z != 0f)
            {
                encapsulatePos = sweepBounds.center;

                if (direction.z > 0f) encapsulatePos.z = sweepBounds.max.z + scaleUnits;
                else encapsulatePos.z = sweepBounds.min.z - scaleUnits;

                sweepBounds.Encapsulate(encapsulatePos);
            }


            if (shrinkBack)
            {
                float shrintPush = 0.99f;
                float shrinkAdd = 0f;

                if (skrinkFurther)
                {
                    shrintPush = 1.025f;
                    shrinkAdd = 0;
                }

                if (direction.x > 0f) sweepBounds.min = new Vector3(sweepBounds.min.x + shrinkAdd + oSize.x * shrintPush, sweepBounds.min.y, sweepBounds.min.z);
                else if (direction.x < 0f) sweepBounds.max = new Vector3(sweepBounds.max.x - (oSize.x * shrintPush) + (-shrinkAdd), sweepBounds.max.y, sweepBounds.max.z);

                if (direction.y > 0f) sweepBounds.min = new Vector3(sweepBounds.min.x, sweepBounds.min.y + shrinkAdd + oSize.y * shrintPush,  sweepBounds.min.z);
                else if (direction.y < 0f) sweepBounds.max = new Vector3(sweepBounds.max.x, sweepBounds.max.y - (oSize.y * shrintPush) + (-shrinkAdd),  sweepBounds.max.z);

                if (direction.z > 0f) sweepBounds.min = new Vector3(sweepBounds.min.x, sweepBounds.min.y, sweepBounds.min.z + shrinkAdd + oSize.z * shrintPush);
                else if (direction.z < 0f) sweepBounds.max = new Vector3(sweepBounds.max.x, sweepBounds.max.y, sweepBounds.max.z - (oSize.z * shrintPush) + (-shrinkAdd) );
            }


            return sweepBounds;
        }


        private Vector3[] _tBoundsDiag = new Vector3[2];
        private Vector3[] _tBounds = new Vector3[4];
        public Vector3[] TransformBounds(Bounds b)
        {
            Matrix4x4 mx = Matrix;
            //Vector3 c = TransformBoundsCenter(b);
            _tBounds[0] = mx.MultiplyPoint3x4(new Vector3(b.min.x, b.center.y, b.min.z));
            _tBounds[1] = mx.MultiplyPoint3x4(new Vector3(b.min.x, b.center.y, b.max.z));
            _tBounds[2] = mx.MultiplyPoint3x4(new Vector3(b.max.x, b.center.y, b.max.z));
            _tBounds[3] = mx.MultiplyPoint3x4(new Vector3(b.max.x, b.center.y, b.min.z));

            //_tBounds[0].y = c.y;
            //_tBounds[1].y = c.y;
            //_tBounds[2].y = c.y;
            //_tBounds[3].y = c.y;

            return _tBounds;
        }


        public Vector3[] TransformBoundsDiag(Bounds b)
        {
            //Vector3 c = TransformBoundsCenter(b);
            _tBoundsDiag[0] = Matrix.MultiplyPoint3x4(new Vector3(b.min.x, b.center.y, b.min.z));
            _tBoundsDiag[1] = Matrix.MultiplyPoint3x4(new Vector3(b.max.x, b.center.y, b.max.z));

            //_tBoundsDiag[0].y = c.y;
            //_tBoundsDiag[1].y = c.y;

            return _tBoundsDiag;
        }


        public Vector3 TransformBoundsCenter(Bounds b)
        {
            return Matrix.MultiplyPoint3x4(b.center);
        }


    }
}