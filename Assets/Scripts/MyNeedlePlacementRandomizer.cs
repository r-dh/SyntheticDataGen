using System;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor.Experimental.SceneManagement;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers.Utilities;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Randomizers.SampleRandomizers
{
    /// <summary>
    /// Creates a 2D layer of of evenly spaced GameObjects from a given list of prefabs
    /// </summary>
    [Serializable]
    [AddRandomizerMenu("Perception/My Needle Placement Randomizer")]
    public class MyNeedlePlacementRandomizer : Randomizer
    {
        /// <summary>
        /// The Z offset component applied to the generated layer of GameObjects
        /// </summary>
        [Tooltip("The Z offset applied to positions of all placed instruments.")]
        public float depth;

        /// <summary>
        /// The Z variation applied to the offset of all placed instruments.
        /// </summary>
        [Tooltip("The Z variation applied to the offset of all placed instruments.")]
        public float depthVariation;
        /// <summary>
        /// The minimum distance between placed instruments
        /// </summary>
        [Tooltip("The minimum distance between the centers of the placed objects.")]
        public float separationDistance = 2f;

        /// <summary>
        /// The size of the 2D area designated for instrument placement
        /// </summary>
        [Tooltip("The width and height of the area in which objects will be placed. These should be positive numbers and sufficiently large in relation with the Separation Distance specified.")]
        public Vector2 placementArea;

        /// <summary>
        /// The list of instruments to place
        /// </summary>
        [Tooltip("The needle to be placed by this Randomizer.")]
        public GameObjectParameter prefabs;

        /// <summary>
        /// The list of prefabs sample and randomly place
        /// </summary>
        [Tooltip("The amount of needles in the frame.")]
        public int needleCount;

        /// <summary>
        /// The amount of images without instruments
        /// </summary>
        [Tooltip("Percentage between 0 and 1 of images without any foreground objects.")]
        public float negativeSamples;

        GameObject m_Container;
        GameObjectOneWayCache m_GameObjectOneWayCache;

        private UniformSampler uniformSampler;
        /// <inheritdoc/>
        protected override void OnAwake()
        {
            uniformSampler = new UniformSampler(0.0f, depthVariation);

            m_Container = new GameObject("Needles");
            m_Container.transform.parent = scenario.transform;
            m_GameObjectOneWayCache = new GameObjectOneWayCache(
                m_Container.transform, prefabs.categories.Select(
                    element => element.Item1).ToArray(), this);
        }

        private (float2 left, float2 right) GenerateLocationsNeedles(ref NativeList<float2> placementSamples)
        {
            // We expect only two instruments, so we keep the leftmost and rightmost point
            if (prefabs.GetCategoryCount() != 2)
            {
                throw new NotImplementedException(
                    "Expected exactly TWO needles");
            }

            float2 min = new float2(placementSamples[0].x, placementSamples[0].y);
            float2 max = new float2(placementSamples[0].x, placementSamples[0].y);

            for (int i = 0; i < placementSamples.Length; i++)
            {
                if (min.x > placementSamples[i].x) min = placementSamples[i];
                if (max.x < placementSamples[i].x) max = placementSamples[i];
            }

            return (min, max);
        }

        /// <summary>
        /// Generates two needles in the foreground layer at the start of each scenario iteration
        /// </summary>
        protected override void OnIterationStart()
        {
            var seed = SamplerState.NextRandomState();
            var placementSamples = PoissonDiskSampling.GenerateSamples(
                placementArea.x,
                placementArea.y,
                separationDistance,
                seed,
                30);

            var offset = new Vector3(placementArea.x, placementArea.y, 0f) * -0.5f;
            offset.z = uniformSampler.Sample() * -1f;

            // ON HOLD: sort generated samples by x coordinate, split resulting list in needleCount parts and sample each location from the resulting partial lists
            // ON HOLD: (Perception v0.8) This gives an error, GetEnumerator() hasn't been implemented by NativeList
            /*var samplesSplit = placementSamples.Select((x, i) => new {value = x, index = i})
                .GroupBy(x => x.index / (placementSamples.Length / prefab.GetCategoryCount()))
                .Select(x => x.Select(z => z.value));*/

            var (left, right) = GenerateLocationsNeedles(ref placementSamples);

            for (int i = 0; i < needleCount; i++)
            {
                float2 sample = (i == 0) ? left : right; // Warning: This only works for two needles
                var instance = m_GameObjectOneWayCache.GetOrInstantiate(
                    prefabs.GetCategory(i));
                instance.transform.localPosition = new Vector3(
                    sample.x, sample.y, depth) + offset;
            }

            placementSamples.Dispose();
        }

        /// <summary>
        /// Deletes generated instruments in the foreground layer after each scenario iteration is complete
        /// </summary>
        protected override void OnIterationEnd()
        {
            m_GameObjectOneWayCache.ResetAllObjects();
        }
    }
}
