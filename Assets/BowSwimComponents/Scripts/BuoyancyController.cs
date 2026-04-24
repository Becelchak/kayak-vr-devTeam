using System;
using Unity.VisualScripting;
using UnityEngine;

public class BuoyancyController : MonoBehaviour
{
    [Header("Physics settings")]
    
    [SerializeField] public Rigidbody _rb;
    [SerializeField] public GameObject left_point;
    [SerializeField] public GameObject right_point;
    [SerializeField] private float mass;
    [SerializeField] private float WaterLevel = 0f;

    [Header("hand settings")]
    [SerializeField] public GameObject left;
    [SerializeField] public GameObject right;

    private GameObject actual_pos;

    public float _power;
    public float _d_power;    

    private BuoyancyPoint[] _buoyancyPoints;
    private float _buoyancyPointCount = 0f;

    public void SetPower(float power, float d_power)
    {
        if(d_power > 0.0f)
            actual_pos = left.transform.position.y < right.transform.position.y ? right_point: left_point;

        _power = power;
        _d_power = d_power;
    }

    void Awake()
    {
        mass = _rb.mass;
        actual_pos = left.transform.position.y < right.transform.position.y ? right_point: left_point;

        _buoyancyPoints = GetComponentsInChildren<BuoyancyPoint>();
        foreach (var point in _buoyancyPoints)
        {
            point.AddToBoard();
        }
        _buoyancyPointCount = _buoyancyPoints.Length;
        WebSocketClient.OnPowerChanged += SetPower;
    }

    void FixedUpdate()
    {
        _rb.AddForceAtPosition(_rb.transform.forward * _power/10f, actual_pos.transform.position, ForceMode.Force);

        foreach (var point in _buoyancyPoints)
        {
            if (point.transform.position.y < WaterLevel)
            {
                var buoyancyForce = mass * Math.Abs(Physics.gravity.y) / _buoyancyPointCount *(1+WaterLevel - point.transform.position.y);
                _rb.AddForceAtPosition(Vector3.up * buoyancyForce, point.transform.position);
            }
        }
    }

    void OnDestroy()
    {
        WebSocketClient.OnPowerChanged -= SetPower;
    }
}
