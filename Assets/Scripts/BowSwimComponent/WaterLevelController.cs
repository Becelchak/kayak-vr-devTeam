using UnityEngine;
using Crest;
using System.Collections;
using System;
using System.Collections.Generic;

public class WaterLevelController : MonoBehaviour
{
    public static Action<GameObject, float> OnWaterLevelChanged;
    private Dictionary<GameObject, float> _objectWaterLevels = new Dictionary<GameObject, float>();

    void Awake()
    {
        OnWaterLevelChanged += WaterLevelUpdatePoint;
    }

    private void WaterLevelUpdatePoint(GameObject _object, float level)
    {
        if(_objectWaterLevels.ContainsKey(_object))
        {
            _objectWaterLevels[_object] = level;
        }
        else
        {
            _objectWaterLevels.Add(_object, level);
        }
    }

    public bool IsUnderWater(GameObject _obj)
    {
        if(_objectWaterLevels.ContainsKey(_obj))
            return _obj.transform.position.y < _objectWaterLevels[_obj];
        return false;
    }

    public float GetWaterLevel(GameObject _obj)
    {
        return _objectWaterLevels[_obj];
    }

    void Update()
    {
        
    }
}