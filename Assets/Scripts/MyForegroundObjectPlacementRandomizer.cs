using System;
using System.Linq;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers.Utilities;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Randomizers.SampleRandomizers
{
    /// <summary>
    /// Creates a 2D layer of of evenly spaced GameObjects from a given list of prefabs
    /// </summary>
    [Serializable]
    [AddRandomizerMenu("Perception/My Foreground Object Placement Randomizer")]
    public class MyForegroundObjectPlacementRandomizer : Randomizer
    {
        /// <summary>
        /// The Z offset component applied to the generated layer of GameObjects
        /// </summary>
        [Tooltip("The Z offset applied to positions of all placed objects.")]
        public float depth;

        /// <summary>
        /// The minimum distance between all placed objects
        /// </summary>
        [Tooltip("The minimum distance between the centers of the placed objects.")]
        public float separationDistance = 2f;

        /// <summary>
        /// The size of the 2D area designated for object placement
        /// </summary>
        [Tooltip("The width and height of the area in which objects will be placed. These should be positive numbers and sufficiently large in relation with the Separation Distance specified.")]
        public Vector2 placementArea;

        /// <summary>
        /// The list of prefabs sample and randomly place
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
            rnd = new System.Random();

            m_Container = new GameObject("Foreground Objects");
            m_Container.transform.parent = scenario.transform;
            m_GameObjectOneWayCache = new GameObjectOneWayCache(
                m_Container.transform, prefabs.categories.Select(element => element.Item1).ToArray(), this);
        }

        /// <summary>
        /// Generates a foreground layer of objects at the start of each scenario iteration
        /// </summary>
        protected override void OnIterationStart()
        {
            var seed = SamplerState.NextRandomState();
            var placementSamples = PoissonDiskSampling.GenerateSamples(
                placementArea.x, placementArea.y, separationDistance, seed);
            var offset = new Vector3(placementArea.x, placementArea.y, 0f) * -0.5f;
            int i = 0;
            //TODO(optional): Random value smaller or eq to objectRange.y as upper limit
            foreach (var sample in placementSamples)
            {
                // Chance to create negative/empty sample
                if (i==0 && objectRange.x==0 && 1-negativeSamples < rnd.NextDouble())
                {
                    break;
                }

                // Upper limit
                if (i > objectRange.y)
                {
                    break;
                }

                var instance = m_GameObjectOneWayCache.GetOrInstantiate(prefabs.Sample());
                instance.transform.localPosition = new Vector3(sample.x, sample.y, depth) + offset;
                //instance.transform.localRotation = mainCamera.transform.rotation;
                i++;
            }

            placementSamples.Dispose();
        }

        /// <summary>
        /// Deletes generated foreground objects after each scenario iteration is complete
        /// </summary>
        protected override void OnIterationEnd()
        {
            m_GameObjectOneWayCache.ResetAllObjects();
        }
    }
}
