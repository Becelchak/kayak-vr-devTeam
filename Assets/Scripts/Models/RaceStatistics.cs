using UnityEngine;

[System.Serializable]
public class RaceStatistics
{
    [Tooltip("Секунды")]
    public float totalTime;
    [Tooltip("Метры (пройденная дистанция)")]
    public float distanceCovered;
    [Tooltip("Средний темп (гребков/мин)")]
    public float avgStrokeRate;
    [Tooltip("Средняя длина гребка (м)")]
    public float avgStrokeLength;
    [Tooltip("Средняя скорость (м/с)")]
    public float avgSpeed;
    [Tooltip("Максимальная скорость")]
    public float maxSpeed;
    [Tooltip("Общее число гребков")]
    public int totalStrokes = 0;
}