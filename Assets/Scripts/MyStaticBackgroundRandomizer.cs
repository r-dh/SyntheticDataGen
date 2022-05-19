using System;
using UnityEngine.UI;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers.Utilities;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Randomizers.SampleRandomizers
{
    [Serializable]
    [AddRandomizerMenu("Perception/My Static Background Randomizer")]
    public class MyStaticBackgroundRandomizer : Randomizer
    {
        public Material backgroundMaterial;
        public SpriteParameter Sprites;

        protected override void OnAwake()
        {
            if (backgroundMaterial == null)
            {
                throw new Exception("Set background material in MyStaticBackgroundRandomizer");
            }
        }

        protected override void OnIterationStart()
        {
            backgroundMaterial.SetTexture("_BaseMap", Sprites.Sample().texture);
        }
    }
}