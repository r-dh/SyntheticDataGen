using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Perception.Randomization.Samplers;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;


public class PermanentRotation : MonoBehaviour
{
    public float DefaultSpeed = 25f;
    public float Delay = 0f;
    public float MaxDuration = 1000f;
    public float MaxAngle = 1000f;
    public float MinAngle = 1000f;
    public Vector3 Axis = Vector3.right;
    public bool FlipAxis = false;
    private float _duration = 0f;
    //private bool _forwardRotation = true;
    //private float _initialAngle = 0f;
    private float _currentAngle = 0f;
    private float _cooldown = 0f;
    private float velocity = 0f;
    private NormalSampler _nsSpeed;

    public void InitPermanentRotation(
        float currentAngle,
        Vector3 axis,
        float speed = 22f,
        float minAngle = -90f,
        float maxAngle = 1000f,
        bool flipAxis = false,
        float delay = 0f,
        float maxDuration = 1000f)
    {
        DefaultSpeed = speed;
        Delay = delay;
        MaxDuration = maxDuration;
        MaxAngle = maxAngle;
        MinAngle = minAngle;
        Axis = axis;
        FlipAxis = flipAxis;
        _currentAngle = currentAngle;
        _nsSpeed = new NormalSampler(0.1f, 1.5f, 1f, 0.5f);
        velocity = DefaultSpeed * _nsSpeed.Sample();
        //_forwardRotation = (UnityEngine.Random.value < 0.5f);
        //_initialAngle = GetCurrentAngle();

    }

    public void ResetTimer()
    {
        _duration = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        //TODO: Random breaks and flipped rotations
        _duration += Time.deltaTime;
        _cooldown -= Time.deltaTime;

        if (_cooldown > 0) return;

        if (Random.value < 0.005f)
        {
            _cooldown = Random.Range(0f, 2f);
            if (Random.value < 0.5f)
            {
                velocity = -1f * Math.Sign(velocity) * DefaultSpeed * _nsSpeed.Sample();
            }
        }

        if (_duration > Delay && _duration < MaxDuration)
        {
            float angle = velocity * Time.deltaTime;
            //float current_angle = GetCurrentAngle();
            float nextAngle = _currentAngle + angle;

            if (nextAngle >= MaxAngle || nextAngle <= MinAngle)
            {
                velocity = -1f * Math.Sign(velocity) * DefaultSpeed * _nsSpeed.Sample();
                _cooldown = Random.Range(0f, 2f);
            }

            GetComponent<Transform>().Rotate(Axis, angle);
            _currentAngle += angle;

        }
    }
}
