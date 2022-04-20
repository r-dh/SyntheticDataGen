using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Perception.Randomization.Parameters;
using UnityEngine.Perception.Randomization.Randomizers.Utilities;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.Randomization.Randomizers.SampleRandomizers
{
    /// <summary>
    /// Creates a 2D layer of of evenly spaced GameObjects from a given list of prefabs
    /// </summary>
    [Serializable]
    [AddRandomizerMenu("Perception/My Instrument Placement Randomizer")]
    public class MyInstrumentPlacementRandomizer : Randomizer
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
        [Tooltip("The list of instruments to be placed by this Randomizer.")]
        public GameObjectParameter prefabs;

        /// <summary>
        /// The list of prefabs sample and randomly place
        /// </summary>
        [Tooltip("The minimum and maximum amount of instruments in the frame.")]
        public Vector2 objectRange;

        /// <summary>
        /// The needle to place
        /// </summary>
        //[Tooltip("The needle to be placed by this Randomizer.")]
        //public GameObjectParameter needlePrefab;


        /// <summary>
        /// The amount of images without instruments
        /// </summary>
        [Tooltip("Percentage between 0 and 1 of images without any foreground objects.")]
        public float negativeSamples;

        GameObject m_Container;
        GameObjectOneWayCache m_GameObjectOneWayCache;

        private UniformSampler uniformSampler;

        private List<Vector3> rotationList = new List<Vector3>();
        /// <inheritdoc/>
        protected override void OnAwake()
        {
            uniformSampler = new UniformSampler(0.0f, depthVariation);

            m_Container = new GameObject("Foreground Objects");
            m_Container.transform.parent = scenario.transform;
            m_GameObjectOneWayCache = new GameObjectOneWayCache(
                m_Container.transform, prefabs.categories.Select(
                    element => element.Item1).ToArray());
        }

        private void PlaceInstruments(ref NativeList<float2> placementSamples)
        {
            // We expect only two instruments, so we keep the leftmost and rightmost point
            if (prefabs.GetCategoryCount() != 2)
            {
                throw new NotImplementedException(
                    "TWO instruments are expected at the same time");
            }

            float2 min = new float2(0, 0);
            float2 max = new float2(0, 0);

            for (int i = 0; i < placementSamples.Length; i++)
            {
                if (min.x > placementSamples[i].x) min = placementSamples[i];
                if (max.x < placementSamples[i].x) max = placementSamples[i];
            }

            placementSamples[0] = min;
            placementSamples[1] = max;
        }

        private void RotateInstruments(ref NativeList<float2> placementSamples, Vector3 offset)
        {
            var rotationSampler = new UniformSampler(0, 360);

            for (int i = 0; i < prefabs.GetCategoryCount(); i++)
            {
                // TODO: check if rotation is uniform

                float2 sample = placementSamples[i];
                var instance = m_GameObjectOneWayCache.GetOrInstantiate(
                    prefabs.GetCategory(i));
                instance.transform.localPosition = new Vector3(
                    sample.x, sample.y, depth) + offset;

                // Rotate the first hinge randomly between 0 - 360 on the x-axis
                float x = rotationSampler.Sample();
                rotationList.Add(new Vector3(x, 0, 0));

                string pathHinge01 = "ORSI_LND_04/B_Root/B_Stick_01/B_Hinge_01";
                instance.transform.Find(pathHinge01).Rotate(rotationList[0]);

                // Rotate the second hinge randomly between -90 - 90 on the z-axis
                var rotation = rotationSampler.Sample();
                rotationList.Add(new Vector3(0, 0, (rotation % 180) - 90));
                instance.transform.Find(pathHinge01 + "/B_Hinge_02").Rotate(rotationList[1]);  //(rotation % 180) - 90


                // Rotate the first driver (tip) randomly between -90 - 90 on the z-axis
                rotation = rotationSampler.Sample();
                var tip1 = (rotation - 180) % 35; //90
                rotationList.Add(new Vector3(0, 0, tip1));
                instance.transform.Find(pathHinge01 + "/B_Hinge_02/B_Driver_01").Rotate(rotationList[2]);

                // Rotate the second driver (tip) randomly between the first driver - 90 on the z-axis

                //if tip1 == -30: tip2 in { -30, -90 }
                // TODO: Check if creating a new sampler doesn't generate the same values as the previous one
                var tip2 = new UniformSampler(-35, tip1).Sample();
                rotationList.Add(new Vector3(0, 0, tip2));
                instance.transform.Find(pathHinge01 + "/B_Hinge_02/B_Driver_02").Rotate(rotationList[3]);
            }
        }

        private void RevertRotationInstruments()
        {

            for (int i = 0; i < prefabs.GetCategoryCount(); i++)
            {
                var instance = m_GameObjectOneWayCache.GetOrInstantiate(
                    prefabs.GetCategory(i));

                string pathHinge01 = "ORSI_LND_04/B_Root/B_Stick_01/B_Hinge_01";
                instance.transform.Find(pathHinge01).Rotate(rotationList[0] * -1);
                instance.transform.Find(pathHinge01 + "/B_Hinge_02").Rotate(rotationList[1] * -1);
                instance.transform.Find(pathHinge01 + "/B_Hinge_02/B_Driver_01").Rotate(rotationList[2] * -1);
                instance.transform.Find(pathHinge01 + "/B_Hinge_02/B_Driver_02").Rotate(rotationList[3] * -1);
            }
        }
        /// <summary>
        /// Generates two instruments in the foreground layer at the start of each scenario iteration
        /// </summary>
        protected override void OnIterationStart()
        {
            var seed = SamplerState.NextRandomState();
            var placementSamples = PoissonDiskSampling.GenerateSamples(
                placementArea.x,
                placementArea.y,
                separationDistance,
                seed,
                50);

            var offset = new Vector3(placementArea.x, placementArea.y, 0f) * -0.5f;
            offset.z = uniformSampler.Sample() * -1f;

            PlaceInstruments(ref placementSamples);

            RotateInstruments(ref placementSamples, offset);

            placementSamples.Dispose();
        }

        /// <summary>
        /// Deletes generated instruments in the foreground layer after each scenario iteration is complete
        /// </summary>
        protected override void OnIterationEnd()
        {
            // TODO(unsuspected behaviour): Rotations are NOT being reverted
            RevertRotationInstruments();

            rotationList.Clear();

            m_GameObjectOneWayCache.ResetAllObjects();
        }
    }
}
