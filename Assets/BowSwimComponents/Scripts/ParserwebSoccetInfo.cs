using System;
using UnityEngine;

[Serializable]
public abstract class WebParserBase : MonoBehaviour
{
    public float _power;
    public float _d_power;
    public abstract float[] Parse(string data);
}

public class ParserwebSoccetInfo: WebParserBase
{

    public override float[] Parse(string message)
    {
        try
        {
            _power = float.Parse(message.Split('\t')[0]);
            _d_power = float.Parse(message.Split('\t')[1]);
            string line = $"{_power},{_d_power}\n";
            return new float[] {_power, _d_power};
        }
        catch
        {
            Debug.LogWarning($"Failed to parse message: {message}");
        }
        return new float[2];
    }
}
