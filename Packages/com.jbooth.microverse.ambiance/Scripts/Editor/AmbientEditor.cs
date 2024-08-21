using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;


namespace JBooth.MicroVerseCore
{
    [CustomEditor(typeof(Ambient))]
    public class AmbientEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // we have to init crap manually, cause unity
            var ambient = target as Ambient;
            if (ambient.randomSounds != null)
            {
                foreach (var rs in ambient.randomSounds)
                {
                    if ((rs.clips == null || rs.clips.Length == 0) &&
                       rs.volume == 0 &&
                       rs.delay == 0 &&
                       rs.pitch == 0 &&
                       rs.spacialization == 0)
                    {
                        rs.delay = 4;
                        rs.pitch = 1;
                        rs.spacialization = 1;
                        rs.volume = 1;
                        rs.playerRadius = 100;
                    }
                }
            }

            DrawDefaultInspector();


        }
    }
}

