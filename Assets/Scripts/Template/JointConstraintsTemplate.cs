using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Perception.Randomization.Samplers;

namespace UnityEngine.Perception.GroundTruth
{
    /// <summary>
    /// A definition of a joint constraint.
    /// </summary>
    [Serializable]
    public class JointConstraint
    {
        /// <summary>
        /// The name of the tool component
        /// </summary>
        public string Name;
        /// <summary>
        /// The axis over which it can rotate
        /// </summary>
        public Vector3 Axis;
        /// <summary>
        /// The maximum negative rotation that can be applied
        /// </summary>
        public float MinimumRotation;
        /// <summary>
        /// The maximum positive rotation that can be applied
        /// </summary>
        public float MaximumRotation;
        /// <summary>
        /// The base speed of the movement during the iteration
        /// </summary>
        public NormalSampler SpeedSampler;
        /// <summary>
        /// The variation of the speed within successive movements
        /// </summary>
        public NormalSampler SpeedVariationSampler;
        /// <summary>
        /// Chance during any given frame to pause animation
        /// </summary>
        public float CooldownChance;
        /// <summary>
        /// Chance to flip the velocity at the end of the aforementioned cooldown
        /// </summary>
        public float FlipVelocityChance;
        /// <summary>
        /// Duration of the animation pause. Also applies when max angle has been reached
        /// </summary>
        public UniformSampler CooldownDuration;
        /// <summary>
        /// Enable this option for the first tip
        /// </summary>
        public bool isTip1;
        /// <summary>
        /// Enable this option for the second tip
        /// </summary>
        public bool isTip2;
    }

    [CreateAssetMenu(fileName = "JointConstraintsTemplate", menuName = "Perception/Joint Constraints Template", order = 2)]
    [Serializable]
    public class JointConstraintsTemplate : ScriptableObject
    {
        /// <summary>
        /// The <see cref="Guid"/> of the template
        /// </summary>
        public string templateID = Guid.NewGuid().ToString();
        /// <summary>
        /// The name of the template
        /// </summary>
        public string templateName;
        /// <summary>
        /// The name of the instrument
        /// </summary>
        public string instrumentName;
        /// <summary>
        /// The name of the tool component
        /// </summary>
        public JointConstraint[] constraints;
    }
}