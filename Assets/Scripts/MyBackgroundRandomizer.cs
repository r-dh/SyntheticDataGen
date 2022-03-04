using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers.Utilities;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Randomizers.SampleRandomizers
{
    [Serializable]
    [AddRandomizerMenu("Perception/My Background Randomizer")]
    public class MyBackgroundRandomizer : Randomizer
    {
        /// <summary>
        /// A Cubemap parameter for sampling random skyboxes
        /// </summary>
        [Tooltip("The list of skyboxes to be placed by this Randomizer.")]
        public CubemapParameter Skyboxes;

        //public int test;
        //private GameObject _container;

        private Material _skyboxMaterial;
        //public List<Cubemap> SkyboxList = new List<Cubemap>();
        private System.Random rnd = new System.Random();
        

        /// <inheritdoc/>
        protected override void OnAwake()
        {
            //SA_BackgroundType.OverrideShuffle(OnShuffle_BackgroundType);

            _skyboxMaterial = new Material(Shader.Find("Skybox/Cubemap"));

            //mainCamera = GameObject.FindWithTag("MainCamera");
            //_container = new GameObject("BackgroundContainer");
            //_container.transform.parent = scenario.transform;
            //_skyboxes = SkyboxList.Select((element) => element.Item1).ToList();
            
        }

        protected override void OnIterationStart()
        {
            _skyboxMaterial.SetTexture("_Tex", Skyboxes.Sample());
            _skyboxMaterial.SetFloat("_Rotation", rnd.Next(0,360));
            

            RenderSettings.skybox = _skyboxMaterial;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;

            DynamicGI.UpdateEnvironment();
            /*
            MainCamera.clearFlags = CameraClearFlags.Skybox;
            _skyboxMaterial.SetTexture("_Tex", SA_SkyboxList.Current);
            RenderSettings.skybox = _skyboxMaterial;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Skybox;
             */
        }

        /// <summary>
        /// Deletes generated foreground objects after each scenario iteration is complete
        /// </summary>
        protected override void OnIterationEnd()
        {
            //mainCamera.transform.Rotate(new Vector3(80, 0, 0));
        }
    }
}
