using System.Collections.Generic;
using UnityEngine;

namespace FIMSpace.Generating
{
    public partial class TileMeshSetup
    {
        public enum EExtraMesh { CustomMesh, CableGenerator, Stacker }
        public EExtraMesh ExtraMesh = EExtraMesh.CustomMesh;
        public EExtraMesh GeneratorType
        {
            get { return ExtraMesh; }
            set { ExtraMesh = value; }
        }


        public Mesh CustomMesh = null;


        void CustomAndExtraQuickUpdate()
        {

        }

    }
}