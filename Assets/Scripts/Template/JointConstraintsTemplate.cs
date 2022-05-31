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
        [Tooltip("The name of the tool component")]
        public string Name;
        /// <summary>
        /// The axis over which it can rotate
        /// </summary>
        [Tooltip("The axis over which it can rotate")] 
        public Vector3 Axis;
        /// <summary>
        /// The maximum negative rotation that can be applied
        /// </summary>
        [Tooltip("The maximum negative rotation that can be applied")] 
        public float MinimumRotation;
        /// <summary>
        /// The maximum positive rotation that can be applied
        /// </summary>
        [Tooltip("The maximum positive rotation that can be applied")] 
        public float MaximumRotation;
        /// <summary>
        /// The base speed of the movement during the iteration
        /// </summary>
        [Tooltip("The base speed of the movement during the iteration")]
        public NormalSampler SpeedSampler;
        /// <summary>
        /// The variation of the speed within successive movements
        /// </summary>
        [Tooltip("The variation of the speed within successive movement")]
        public NormalSampler SpeedVariationSampler;
        /// <summary>
        /// Chance during any given frame to pause animation
        /// </summary>
        [Tooltip("Chance during any given frame to pause animation")]
        public float CooldownChance;
        /// <summary>
        /// Chance to flip the velocity at the end of the aforementioned cooldown
        /// </summary>
        [Tooltip("Chance to flip the velocity at the end of the aforementioned cooldown")]
        public float FlipVelocityChance;
        /// <summary>
        /// Duration of the animation pause. Also applies when max angle has been reached
        /// </summary>
        [Tooltip("Duration of the animation pause. Also applies when max angle has been reached")]
        public UniformSampler CooldownDuration;
        /// <summary>
        /// Enable this option for the first tip
        /// </summary>
        [Tooltip("Enable this option for the first tip")]
        public bool isTip1;
        /// <summary>
        /// Enable this option for the second tip
        /// </summary>
        [Tooltip("Enable this option for the second tip")]
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