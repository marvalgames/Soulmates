using UnityEditor;
using UnityEngine;

namespace JBooth.MicroVerseCore
{
    /// <summary>
    /// Common menu helper functions
    /// </summary>
    public static class MenuUtil
    {
        /// <summary>
        /// Transfer noise from stamps to sibling stamps, ie gameobjects which have the same parent transform in the hierarchy
        /// </summary>
        /// <param name="command"></param>
        [MenuItem("CONTEXT/Stamp/MicroVerse/Transfer Noise to Siblings")]
        public static void TransferNoiseToSiblings(MenuCommand command)
        {
            TransferNoiseToSiblings((Stamp)command.context);
        }

        [MenuItem("CONTEXT/Stamp/MicroVerse/Transfer Noise to Siblings", true)]
        public static bool CanTransferNoiseToSiblings(MenuCommand command)
        {
            if (command.context is TextureStamp || command.context is OcclusionStamp)
                return true;

#if __MICROVERSE_VEGETATION__

            if (command.context is TreeStamp || command.context is DetailStamp || command.context is ClearStamp)
                return true;

#endif

#if __MICROVERSE_OBJECTS__

            if (command.context is ObjectStamp)
                return true;

#endif

            return false;
        }


        /// <summary>
        /// Take the transform of the stamp and transfer the noise of the stamp's filterset to all the siblings.
        /// eg if you have a detail stamp transform parent with several grass detail stamp children you can transfer the settings from one grass detail stamp to all the others
        /// </summary>
        /// <param name="transform"></param>
        public static void TransferNoiseToSiblings( Stamp currentStamp)
        {
            Transform parent = currentStamp.transform.parent;

            if (parent == null)
                return;

            // Stamp currentStamp = transform.GetComponent<Stamp>();

            if (currentStamp == null)
                return;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);

                if (child == currentStamp.transform)
                    continue;

                Stamp stamp = child.GetComponent<Stamp>();

                if (stamp == null)
                    continue;

                FilterSet currentFilterSet = currentStamp.GetFilterSet();

                if (currentFilterSet == null)
                    continue;

                FilterSet otherFilterSet = stamp.GetFilterSet();

                if (otherFilterSet == null)
                    continue;

                // clone and transfer data
                otherFilterSet.weightNoise = currentFilterSet.weightNoise.Clone() as Noise;
                otherFilterSet.weight2Noise = currentFilterSet.weight2Noise.Clone() as Noise;
                otherFilterSet.weight3Noise = currentFilterSet.weight3Noise.Clone() as Noise;

            }

            MicroVerse.instance.Invalidate();
        }
    }
}