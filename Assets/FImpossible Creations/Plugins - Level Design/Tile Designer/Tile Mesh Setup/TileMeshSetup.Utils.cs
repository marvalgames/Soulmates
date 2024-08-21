using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Generating
{
    public partial class TileMeshSetup
    {
        public List<CurvePoint> GetCurve1()
        {
            if (GenTechnique == EMeshGenerator.Loft) return _loft_depth;
            else if (GenTechnique == EMeshGenerator.Lathe) return _lathe_points;
            else if (GenTechnique == EMeshGenerator.Extrude) return _extrude_curve;
            else if (GenTechnique == EMeshGenerator.Sweep) return _sweep_path;
            return null;
        }

        public List<CurvePoint> GetCurve2()
        {
            if (GenTechnique == EMeshGenerator.Loft) return _loft_distribute;
            else if (GenTechnique == EMeshGenerator.Lathe) return _lathe_points;
            else if (GenTechnique == EMeshGenerator.Extrude) return _extrude_curve;
            else if (GenTechnique == EMeshGenerator.Sweep) return _sweep_shape;
            return null;
        }

    }
}