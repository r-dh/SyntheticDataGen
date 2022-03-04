using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers.Utilities;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Randomizers.SampleRandomizers
{
    /// <summary>
    /// Creates multiple layers of evenly distributed but randomly placed objects
    /// </summary>
    [Serializable]
    [AddRandomizerMenu("Perception/My Background Object Placement Randomizer")]
    public class BackgroundObjectPlacementRandomizer : Randomizer
    {
        /// <summary>
        /// The Z offset component applied to all generated background layers
        /// </summary>
        [Tooltip("The Z offset applied to positions of all placed objects.")]
        public float depth;

        /// <summary>
        /// The number of background layers to generate
        /// </summary>
        [Tooltip("The number of background layers to generate.")]
        public int layerCount = 2;

        /// <summary>
        /// The minimum distance between placed background objects
        /// </summary>
        [Tooltip("The minimum distance between the centers of the placed objects.")]
        public float separationDistance = 2f;

        /// <summary>
        /// The 2D size of the generated background layers
        /// </summary>
        [Tooltip("The width and height of the area in which objects will be placed. These should be positive numbers and sufficiently large in relation with the Separation Distance specified.")]
        public Vector2 placementArea;

        /// <summary>
        /// A categorical parameter for sampling random prefabs to place
        /// </summary>
        [Tooltip("The list of Prefabs to be placed by this Randomizer.")]
        public GameObjectParameter prefabs;

        /// <summary>
        /// The list of prefabs sample and randomly place
        /// </summary>
        [Tooltip("The minimum and maximum amount of objects in the frame.")]
        public Vector2 objectRange;

        /// <summary>
        /// The list of prefabs sample and randomly place
        /// </summary>
        [Tooltip("Percentage between 0 and 1 of images without any foreground objects.")]
        public float negativeSamples;

        GameObject m_Container;
        GameObjectOneWayCache m_GameObjectOneWayCache;
        private System.Random rnd;

        /// <inheritdoc/>
        protected override void OnAwake()
        {
            rnd = new System.Random(132);
            m_Container = new GameObject("BackgroundContainer");
            m_Container.transform.parent = scenario.transform;
            m_GameObjectOneWayCache = new GameObjectOneWayCache(
                m_Container.transform, prefabs.categories.Select((element) => element.Item1).ToArray());
        }

        /// <summary>
        /// Generates background layers of objects at the start of each scenario iteration
        /// </summary>
        protected override void OnIterationStart()
        {
            int j = 0;
            for (var i = 0; i < layerCount; i++)
            {
                var seed = SamplerState.NextRandomState();
                var placementSamples = PoissonDiskSampling.GenerateSamples(
                    placementArea.x, placementArea.y, separationDistance, seed);
                var offset = new Vector3(placementArea.x, placementArea.y, 0f) * -0.5f;
                //Random value smaller or eq to objectRange.y as upper limit
                int maxObjects = rnd.Next((int)objectRange.y+1);

                // Chance to create negative/empty sample
                if (i == 0 && objectRange.x == 0 && 1 - negativeSamples < rnd.NextDouble())
                {
                    i = layerCount;
                    placementSamples.Dispose();
                    break;
                }

                //var smallSample;// = placementSamples.OrderBy(item => rnd.Next()).Take((int)objectRange.y);
                int n = placementSamples.Length;
                while (n > 1)
                {
                    n--;
                    int k = rnd.Next(n + 1);
                    Unity.Mathematics.float2 value = placementSamples[k];
                    placementSamples[k] = placementSamples[n];
                    placementSamples[n] = value;
                }
                               
                foreach (var sample in placementSamples) //placementSamples
                {
                    // Upper limit
                    if (j > maxObjects)
                    {
                        break;
                    }

                    var instance = m_GameObjectOneWayCache.GetOrInstantiate(prefabs.Sample());
                    instance.transform.localPosition = new Vector3(sample.x, sample.y, separationDistance * i + depth) + offset;
                    j++;
                }
                placementSamples.Dispose();
            }
        }

        /// <summary>
        /// Deletes generated background objects after each scenario iteration is complete
        /// </summary>
        protected override void OnIterationEnd()
        {
            m_GameObjectOneWayCache.ResetAllObjects();
        }
    }
}
