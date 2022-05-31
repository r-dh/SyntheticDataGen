using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Perception.Randomization.Samplers;
using Random = UnityEngine.Random;

/// <summary>
/// Moves gameobject constantly within certain constraints.
/// </summary>
public class RandomMovement : MonoBehaviour
{
    public float DefaultSpeed = 5f;

    private float _cooldown = 0f;

    private float _currentSpeed = 0f;
    Vector3 _targetPos;

    private NormalSampler _nsSpeed;
    private NormalSampler _horizontalSampler;
    private NormalSampler _verticalSampler;

    public void Init(
        float2 horizontalBounds,
        float2 verticalBounds,
        float speed = 1.8f)
    {
        DefaultSpeed = speed;
        _nsSpeed = new NormalSampler(0.1f, 1.5f, 1f, 1f, true, 0.1f, 1.5f);
        _currentSpeed = DefaultSpeed * _nsSpeed.Sample(); 
        _horizontalSampler = new NormalSampler(horizontalBounds.x, horizontalBounds.y, (horizontalBounds.x + horizontalBounds.y)/2, 1f);
        _verticalSampler = new NormalSampler(verticalBounds.x, verticalBounds.y, (verticalBounds.x + verticalBounds.y) / 2, 2f);
        _targetPos = GetNewTargetPos();
    }

    private Vector3 GetNewTargetPos()
    {
        return new Vector3(_horizontalSampler.Sample(), _verticalSampler.Sample(),
            GetComponent<Transform>().position.z);
    }

    void Update()
    {
        _cooldown -= Time.deltaTime;
        if (_cooldown > 0) return;
        
        Vector3 localPos = GetComponent<Transform>().position;
        float dist = Vector3.Distance(_targetPos, localPos);
        
        if (dist < 1f)
        {
            _cooldown = Random.Range(0.1f, 2f);
            _targetPos = GetNewTargetPos();
            _currentSpeed = DefaultSpeed * _nsSpeed.Sample();
            return;
        }

        Vector3 instVelocity = (_targetPos - localPos).normalized * _currentSpeed * Time.deltaTime;
        instVelocity.z = 0f;
        GetComponent<Transform>().Translate(instVelocity, Space.World);
    }
}
