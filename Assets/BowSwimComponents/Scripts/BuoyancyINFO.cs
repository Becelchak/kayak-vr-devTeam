using System;
using UnityEngine;

public class BuoyancyINFO : MonoBehaviour
{
    public static Action<Vector3> VelocityInfoUpdated;
    private Rigidbody _rb;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    void FixedUpdate()
    {
        VelocityInfoUpdated.Invoke(_rb.linearVelocity);
        print(_rb.linearVelocity);
    }
}
