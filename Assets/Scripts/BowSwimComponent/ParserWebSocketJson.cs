using System;
using UnityEngine;

[Serializable]
public class WebSocketData
{
    public float baseValue;
    public float legValue;
    public float ts;
}

public class ParserWebSocketJson : WebParserBase
{
    public override float[] Parse(string message)
    {
        try
        {
            // Парсинг JSON
            var data = JsonUtility.FromJson<WebSocketData>(message);
            _power = data.baseValue;   // base = power
            _d_power = data.legValue;  // leg = d_power
            return new float[] { _power, _d_power };
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to parse JSON: {message}, error: {e.Message}");
        }
        return new float[2];
    }
}