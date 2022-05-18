using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
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
        private List<Quaternion> initialRotations = new List<Quaternion>();
        private List<PermanentRotation> animatedRotations = new List<PermanentRotation>();
        private List<GameObject> tools = new List<GameObject>();

        /// <inheritdoc/>
        protected override void OnAwake()
        {
            uniformSampler = new UniformSampler(0.0f, depthVariation);

            m_Container = new GameObject("Foreground Objects");
            m_Container.transform.parent = scenario.transform;
            m_GameObjectOneWayCache = new GameObjectOneWayCache(
                m_Container.transform, prefabs.categories.Select(
                    element => element.Item1).ToArray(), this);
        }

        /// <summary>
        /// Select most extreme points out of a list.
        /// </summary>
        private (float2 left, float2 right) GenerateLocationsInstruments(ref NativeList<float2> placementSamples)
        {
            // We expect only two instruments, so we keep the leftmost and rightmost point
            if (prefabs.GetCategoryCount() != 2)
            {
                throw new NotImplementedException(
                    "TWO instruments are expected at the same time");
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

        private void PlaceAndRotateInstruments(float2 left, float2 right, Vector3 offset)
        {
            var rotationSampler = new UniformSampler(0, 360);

            for (int i = 0; i < prefabs.GetCategoryCount(); i++)
            {
                float2 sample = (i == 0) ? left : right;

                var instance = m_GameObjectOneWayCache.GetOrInstantiate(
                    prefabs.GetCategory(i));
                instance.transform.localPosition = new Vector3(
                    sample.x, sample.y, depth) + offset;

                RandomMovement rm = instance.AddComponent<RandomMovement>();
                float min = Math.Min(sample.x * 0.9f, sample.x * 1.1f) + offset.x;
                float max = Math.Max(sample.x * 0.9f, sample.x * 1.1f) + offset.x;

                rm.Init(new float2(min, max), new float2(placementArea.y * -0.5f, placementArea.y * 0.5f));
                tools.Add(instance);

                // TODO(low): check if rotation is truly uniform
                // Rotate the first hinge randomly between 0° - 360° on the x-axis
                float x = rotationSampler.Sample();
                rotationList.Add(new Vector3(x, 0, 0));

                // Rotate the second hinge randomly between -80° - 80° on the z-axis
                var rotation = rotationSampler.Sample();
                rotationList.Add(new Vector3(0, 0, (rotation % 160) - 80)); //(rotation % 180) - 90

                // Rotate the first driver (tip) randomly between -90° - 90° on the z-axis
                rotation = rotationSampler.Sample();
                var tip1 = (rotation - 180) % 90;
                rotationList.Add(new Vector3(0, 0, tip1));

                // Rotate the second driver (tip) randomly between -90° and the first driver (inverted) on the z-axis
                var tip2 = new UniformSampler(-90, tip1 * -1f).Sample();
                rotationList.Add(new Vector3(0, 0, tip2));

                NormalSampler nsSpeed = new NormalSampler(1, 40, 25, 0.5f);

                string instrument = instance.transform.GetChild(0).name;
                string pathHinge01 = $"{instrument}/B_Root/B_Shaft_01/B_Hinge_01";
                var hinge01 = instance.transform.Find(pathHinge01);
                initialRotations.Add(hinge01.localRotation);
                hinge01.Rotate(rotationList[0 + i * 4]);
                var prh01 = hinge01.gameObject.AddComponent<PermanentRotation>();
                prh01.InitPermanentRotation(0f, Vector3.right);
                animatedRotations.Add(prh01);

                var hinge02 = instance.transform.Find(pathHinge01 + "/B_Hinge_02");
                initialRotations.Add(hinge02.localRotation);
                hinge02.Rotate(rotationList[1 + i * 4]);
                var prh02 = hinge02.gameObject.AddComponent<PermanentRotation>();
                float currentPos = rotationList[1 + i * 4].z;

                prh02.InitPermanentRotation(currentPos, Vector3.forward, nsSpeed.Sample(), -80f, 80f, true);
                animatedRotations.Add(prh02);
                // TODO: Randomly choose between forward/backward and then calculate maxangle
                // TODO: randomly choose true/false for flip axis
                NormalSampler nsMinAngle = new NormalSampler(-90f, tip1, (-90f + tip1)/2, 1f, true, -90f, tip1);

                var driver01 = instance.transform.Find(pathHinge01 + "/B_Hinge_02").GetChild(2);
                initialRotations.Add(driver01.localRotation);
                driver01.Rotate(rotationList[2 + i * 4]);
                var prd01 = driver01.gameObject.AddComponent<PermanentRotation>();
                //TODO: This is probably riddled with unwanted behaviour
                //TODO: decide on centerpoint
                float centerpoint = (tip1 - tip2) / 2;
                prd01.InitPermanentRotation(tip1, Vector3.forward, nsSpeed.Sample(), nsMinAngle.Sample(), centerpoint, true);
                animatedRotations.Add(prd01);

                var driver02 = instance.transform.Find(pathHinge01 + "/B_Hinge_02").GetChild(3);
                initialRotations.Add(driver02.localRotation);
                driver02.Rotate(rotationList[3 + i * 4]);
                var prd02 = driver02.gameObject.AddComponent<PermanentRotation>();
                //TODO: This is probably riddled with unwanted behaviour
                prd02.InitPermanentRotation(tip2, Vector3.forward, nsSpeed.Sample(), -90f, -centerpoint, true);
                animatedRotations.Add(prd02);

            }
        }

        /// <summary>
        /// Resets the various components to their original rotations
        /// </summary>
        private void RevertRotationInstruments()
        {
            for (int i = 0; i < tools.Count(); i++)
            {
                var instance = tools[i];

                string instrument = instance.transform.GetChild(0).name;
                string pathHinge01 = $"{instrument}/B_Root/B_Shaft_01/B_Hinge_01";
                instance.transform.Find(pathHinge01).localRotation = initialRotations[0 + i * 4];
                instance.transform.Find(pathHinge01 + "/B_Hinge_02").localRotation = initialRotations[1 + i * 4];
                instance.transform.Find(pathHinge01 + "/B_Hinge_02").GetChild(2).localRotation = initialRotations[2 + i * 4];
                instance.transform.Find(pathHinge01 + "/B_Hinge_02").GetChild(3).localRotation = initialRotations[3 + i * 4];
            }

            tools.Clear();
            rotationList.Clear();
            initialRotations.Clear();
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

            var (left, right) = GenerateLocationsInstruments(ref placementSamples);

            PlaceAndRotateInstruments(left, right, offset);

            placementSamples.Dispose();
        }

        /// <summary>
        /// Deletes generated instruments in the foreground layer after each scenario iteration is complete
        /// </summary>
        protected override void OnIterationEnd()
        {
            RevertRotationInstruments();
            foreach (var permanentRotation in animatedRotations)
            {
                Object.Destroy(permanentRotation);
            }
            animatedRotations.Clear();
            m_GameObjectOneWayCache.ResetAllObjects();
        }
    }
}
