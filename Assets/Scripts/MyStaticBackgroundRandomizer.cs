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
        public Image Background;
        public SpriteParameter Sprites;

        protected override void OnAwake()
        {
            if (Background == null)
            {
                throw new Exception("Set background in MyStaticBackgroundRandomizer");
            }
 // Sprites.SetOptions();
        }

        protected override void OnIterationStart()
        {
            Background.sprite = Sprites.Sample();
        }
    }
}