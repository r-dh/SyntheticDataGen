using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers.Utilities;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Randomizers.SampleRandomizers
{
    [Serializable]
    [AddRandomizerMenu("Perception/My Camera Pitch Randomizer")]
    public class MyCameraPitchRandomizer : Randomizer
    {
        [Tooltip("Tip: Select Gaussian to enerate values between -1 and 1.")]
        public FloatParameter pitchParameter;

        private GameObject mainCamera;

        protected override void OnAwake()
        {
            mainCamera = GameObject.FindWithTag("MainCamera");
        }
        protected override void OnIterationStart()
        {
            Vector3 rotation = mainCamera.transform.rotation.eulerAngles;
            mainCamera.transform.rotation = Quaternion.identity;
            mainCamera.transform.Rotate(pitchParameter.Sample() * 90, rotation.y, rotation.z, Space.World); //pitchParameter.Sample() * 90   
        }

    }
}