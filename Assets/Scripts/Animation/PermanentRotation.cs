using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Perception.GroundTruth;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;


public class PermanentRotation : MonoBehaviour
{
    private float _defaultSpeed = 25f;
    private float _delay = 0f;
    private float _maxDuration = 1000f;
    private float _maxAngle = 1000f;
    private float _minAngle = 1000f;
    private Vector3 _axis = Vector3.right;
    private float _cooldownChance = 0.005f;
    private float _duration = 0f;
    private float _currentAngle = 0f;
    private float _cooldown = 0f;
    private float velocity = 0f;
    private float _flipVelocityChance = 0.5f;
    private NormalSampler _nsSpeedVariation;
    private UniformSampler _cooldownSampler;
    private UniformSampler _randomSampler = new UniformSampler(0, 1);

    public void InitPermanentRotation(
        float currentAngle,
        JointConstraint constraint,
        float minAngle = 0f,
        float maxAngle = 1000f,
        float delay = 0f,
        float maxDuration = 1000f
        )
    {
        _defaultSpeed = constraint.SpeedSampler.Sample();
        _delay = delay;
        _maxDuration = maxDuration;
        _minAngle = minAngle; // constraint.MinimumRotation;
        _maxAngle = maxAngle; // constraint.MaximumRotation;
        _axis = constraint.Axis;
        _flipVelocityChance = constraint.FlipVelocityChance;
        _currentAngle = currentAngle;
        _cooldownChance = constraint.CooldownChance;
        _cooldownSampler = constraint.CooldownDuration;
        _nsSpeedVariation = constraint.SpeedVariationSampler; //new NormalSampler(0.1f, 1.5f, 1f, 0.5f);
        velocity = _defaultSpeed * _nsSpeedVariation.Sample();
    }

    public void ResetTimer()
    {
        _duration = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        _duration += Time.deltaTime;
        _cooldown -= Time.deltaTime;

        if (_cooldown > 0) return;
        
        if (_randomSampler.Sample() < _cooldownChance)
        {
            _cooldown = _cooldownSampler.Sample();
            if (_randomSampler.Sample() < _flipVelocityChance)
            {
                velocity = -1f * Math.Sign(velocity) * _defaultSpeed * _nsSpeedVariation.Sample();
            }
        }

        if (_duration > _delay && _duration < _maxDuration)
        {
            float angle = velocity * Time.deltaTime;
            float nextAngle = _currentAngle + angle;

            if (nextAngle >= _maxAngle || nextAngle <= _minAngle)
            {
                velocity = -1f * Math.Sign(velocity) * _defaultSpeed * _nsSpeedVariation.Sample();
                _cooldown = _cooldownSampler.Sample();
            }

            GetComponent<Transform>().Rotate(_axis, angle);
            _currentAngle += angle;
        }
    }
}
