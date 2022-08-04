using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
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
        /// The joint constraints to enforce on the instruments
        /// </summary>
        [Tooltip("The constraints enforced on the rotations.")]
        public JointConstraintsTemplate jointConstrains;


        private GameObject m_Container;
        private GameObjectOneWayCache m_GameObjectOneWayCache;

        private UniformSampler _uniformSampler;

        private Dictionary<string, Quaternion> _initialRotations = new Dictionary<string, Quaternion>();
        private List<PermanentRotation> _animatedRotations = new List<PermanentRotation>();
        private List<GameObject> _tools = new List<GameObject>();
        private Dictionary<string, JointConstraint> _jcDict = new Dictionary<string, JointConstraint>(); // Not ordered, could lead to future bugs
        /// <inheritdoc/>
        protected override void OnAwake()
        {
            _uniformSampler = new UniformSampler(0.0f, depthVariation);

            m_Container = new GameObject("Foreground Objects");
            m_Container.transform.parent = scenario.transform;
            m_GameObjectOneWayCache = new GameObjectOneWayCache(
                m_Container.transform, prefabs.categories.Select(
                    element => element.Item1).ToArray(), this);

            foreach (var constraint in jointConstrains.constraints)
            {
                _jcDict.Add(constraint.Name, constraint);
            }
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
            for (int i = 0; i < prefabs.GetCategoryCount(); i++)
            {
                float2 sample = (i == 0) ? left : right;

                var instance = m_GameObjectOneWayCache.GetOrInstantiate(prefabs.GetCategory(i));
                instance.transform.localPosition = new Vector3(sample.x, sample.y, depth) + offset;

                RandomMovement rm = instance.AddComponent<RandomMovement>();
                float min = Math.Min(sample.x * 0.9f, sample.x * 1.1f) + offset.x;
                float max = Math.Max(sample.x * 0.9f, sample.x * 1.1f) + offset.x;

                rm.Init(new float2(min, max), new float2(placementArea.y * -0.5f, placementArea.y * 0.5f));
                _tools.Add(instance);

                float tip1 = 0f, tip2 = 0f, centerpoint = 0f;

                _initialRotations.Add(instance.name, instance.transform.localRotation);

                foreach (var hinge in instance.GetComponentsInChildren<Transform>())
                {
                    // TODO: check if this is in order
                    if (_jcDict.ContainsKey(hinge.name))
                    {
                        var constraint = _jcDict[hinge.name];
                        var rotationSampler = new UniformSampler(constraint.MinimumRotation, constraint.MaximumRotation);
                        var initialOffset = rotationSampler.Sample();
                        
                        _initialRotations.Add($"{instance.name}_{hinge.name}", hinge.localRotation);

                        if (!constraint.isTip2)
                        {
                            hinge.Rotate(initialOffset * constraint.Axis);
                        }

                        var pr = hinge.gameObject.AddComponent<PermanentRotation>();
                        
                        if (constraint.isTip1)  {
                            tip1 = initialOffset;
                            tip2 = new UniformSampler(constraint.MinimumRotation, tip1 * -1f).Sample(); // min is used from tip1, we expect this to be symmetrical
                            centerpoint = (tip1 - tip2) / 2;

                            pr.InitPermanentRotation(initialOffset, constraint, constraint.MinimumRotation, centerpoint);
                        } else if (constraint.isTip2) {
                            hinge.Rotate(tip2 * constraint.Axis);
                            pr.InitPermanentRotation(tip2, constraint, constraint.MinimumRotation, -centerpoint);
                        } else {
                            pr.InitPermanentRotation(initialOffset, constraint, constraint.MinimumRotation, constraint.MaximumRotation);
                        }

                        _animatedRotations.Add(pr);
                    }
                }
            }
        }
        /// <summary>
        /// Resets the various components to their original rotations
        /// </summary>
        private void RevertRotationInstruments()
        {
            for (int i = 0; i < _tools.Count(); i++)
            {
                var instance = _tools[i];

                if (_initialRotations.ContainsKey(instance.name)) // Root object is not included in GetComponentsInChildren
                {
                    instance.transform.localRotation = _initialRotations[instance.name];
                }

                foreach (var hinge in instance.GetComponentsInChildren<Transform>())
                {
                    string rotationKey = $"{instance.name}_{hinge.name}";
                    if (_initialRotations.ContainsKey(rotationKey))
                    {
                        hinge.localRotation = _initialRotations[rotationKey];
                    }
                }
                Object.Destroy(instance.GetComponent<RandomMovement>());
            }

            _tools.Clear();
            _initialRotations.Clear();
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
            offset.z = _uniformSampler.Sample() * -1f;

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
            foreach (var permanentRotation in _animatedRotations)
            {
                Object.Destroy(permanentRotation);
            }
            _animatedRotations.Clear();
            m_GameObjectOneWayCache.ResetAllObjects();
        }
    }
}
