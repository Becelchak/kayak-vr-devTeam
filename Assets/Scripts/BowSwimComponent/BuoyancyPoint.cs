using System;
using UnityEngine;

public class BuoyancyPoint : MonoBehaviour
{
    public enum BuoyancyPointInitState
    {
        None,
        initial
    }

    private BuoyancyPointInitState _initState = BuoyancyPointInitState.None;

    [Header("Gizmos settings")]
    [SerializeField] private float ShapeSize = 0.05f;

    public void AddToBoard()
    {
        _initState = BuoyancyPointInitState.initial;
    }

    void OnDrawGizmos()
    {
        switch (_initState)
        {
            case BuoyancyPointInitState.None:
                Gizmos.color = Color.red;
                Gizmos.DrawSphere(transform.position, ShapeSize);
                break;
            case BuoyancyPointInitState.initial:
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(transform.position, ShapeSize);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
