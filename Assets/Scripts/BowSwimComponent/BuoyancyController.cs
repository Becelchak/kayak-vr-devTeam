using System;
using Unity.VisualScripting;
using UnityEngine;

public class BuoyancyController : MonoBehaviour
{
    [Header("Physics settings")]
    
    [SerializeField] public Rigidbody _rb;
    [SerializeField] public GameObject force_point;
    [SerializeField] private float mass;
    [SerializeField] private WaterLevelController WaterLevel;

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
        _power = power;
        _d_power = d_power;         
    }

    void Awake()
    {
        mass = _rb.mass;
        //actual_pos = left.transform.position.y < right.transform.position.y ? right_point: left_point;

        _buoyancyPoints = GetComponentsInChildren<BuoyancyPoint>();
        foreach (var point in _buoyancyPoints)
        {
            point.AddToBoard();
        }
        _buoyancyPointCount = _buoyancyPoints.Length;
        WebSocketClient.OnNewPower += SetPower;
    }

    void FixedUpdate()
    {
        if(_power > 0.0f)
            _rb.AddForceAtPosition(_rb.transform.forward * _power/2, force_point.transform.position, ForceMode.Force);

        foreach (var point in _buoyancyPoints)
        {
            if (WaterLevel.IsUnderWater(point.gameObject))
            {
                var buoyancyForce = mass * Math.Abs(Physics.gravity.y) / 
                _buoyancyPointCount * (1 + WaterLevel.GetWaterLevel(point.gameObject) - point.transform.position.y);

                _rb.AddForceAtPosition(Vector3.up * buoyancyForce, point.transform.position);
            }
        }
    }

    void OnDestroy()
    {
        WebSocketClient.OnNewPower -= SetPower;
    }
}