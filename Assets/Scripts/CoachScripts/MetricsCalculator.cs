using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// Расчёт метрик гребли: темп, длина гребка, скорость.
/// Подписывается на событие WebSocketClient.OnPowerChanged.
/// </summary>
public class MetricsCalculator : MonoBehaviour
{
    [Header("Kayak Parameters")]
    [Tooltip("Rigidbody каяка (для получения скорости)")]
    [SerializeField] private Rigidbody kayakBody;
    [Tooltip("Water drag coefficient (F = drag * v)")]
    [SerializeField] private float waterDrag = 50f;

    [Header("Stroke Detection")]
    [Tooltip("Порог power для гребка")]
    [SerializeField] private float powerThreshold = 5f;
    [Tooltip("Множитель для определения длины гребка (меньше = раньше конец гребка)")]
    [SerializeField] private float powerOffCoeficent = 0.5f;
    [Tooltip("Минимальное время между гребками")]
    [SerializeField] private float minStrokeInterval = 0.3f;
    [Tooltip("Время после последнего гребка для фиксации нового (в секундах)")]
    [SerializeField] private float minTimeCheckStreetrate = 1f;

    [Header("Дебаг (Console Output)")]
    [Tooltip("Выводить ли метрики в консоль")]
    [SerializeField] private bool logToConsole = true;

    #region Output metrics
    /// <summary>Темп гребков (гребков/минуту).</summary>
    public float StrokeRate { get; private set; }
    /// <summary>Длина гребка (метры).</summary>
    public float StrokeLength { get; private set; }
    /// <summary>Скорость каяка (м/с).</summary>
    public float Speed { get; private set; }
    #endregion

    # region Private fields for calculation
    private float lastStrokeTime = -100f;
    private float lastStrokeRealTime = -100f;
    private int strokeCount = 0;
    private float strokeStartTime = 0f;
    private float strokeStartVelocity = 0f;
    private float currentPower = 0f;
    private float currentDPower = 0f;
    private float tempoDecayRate = 5f;
    private Queue<float> recentStrokeIntervals = new Queue<float>();
    /// <summary>Событие обновления метрик (темп, длина, скорость).</summary>
    public event Action<float, float, float> OnMetricsUpdated;
    /// <summary>Событие начала гребка.</summary>
    public event Action OnStrokeStarted;
    /// <summary>Событие окончания гребка.</summary>
    public event Action OnStrokeEnded;

    private bool isStrokeInProgress = false;
    #endregion

    private void OnEnable()
    {
        WebSocketClient.OnPowerChanged += OnPowerChanged;
    }

    private void OnDisable()
    {
        WebSocketClient.OnPowerChanged -= OnPowerChanged;
    }

    private void Update()
    {
        if (kayakBody != null)
            Speed = kayakBody.linearVelocity.magnitude;
        else
            Speed = EstimateSpeedFromPower();
        if (Time.time - lastStrokeRealTime > minTimeCheckStreetrate)
        {
            StrokeRate = Mathf.Max(0, StrokeRate - tempoDecayRate * Time.deltaTime);
        }

        if (logToConsole)
        {
        }
    }

    private void OnPowerChanged(float power, float dPower)
    {
        currentPower = power;
        currentDPower = dPower;

        if (power > powerThreshold && Time.time - lastStrokeTime > minStrokeInterval)
        {
            var now = Time.time;

            if (!isStrokeInProgress)
            {
                isStrokeInProgress = true;
                OnStrokeStarted?.Invoke();
            }

            if (strokeCount > 0)
            {
                var interval = now - lastStrokeTime;
                AddStrokeInterval(interval);
            }

            lastStrokeTime = now;
            strokeCount++;

            strokeStartTime = now;
            strokeStartVelocity = Speed;

        }

        if(isStrokeInProgress && power < powerThreshold * powerOffCoeficent)
        {
            if(strokeStartTime > 0 && Time.time - strokeStartTime > 0.2f)
            {
                float strokeDuration = Time.time - strokeStartTime;
                float deltaV = Speed - strokeStartVelocity;
                strokeStartTime = 0f;
            }
            isStrokeInProgress = false;
            OnStrokeEnded?.Invoke();
        }

        OnMetricsUpdated?.Invoke(StrokeRate, StrokeLength, Speed);
    }

    public void SetStrokeLength(float length)
    {
        StrokeLength = length;
    }

    private void AddStrokeInterval(float interval)
    {
        recentStrokeIntervals.Enqueue(interval);
        while (recentStrokeIntervals.Count > 10)
            recentStrokeIntervals.Dequeue();

        float sum = 0f;
        foreach (var i in recentStrokeIntervals)
            sum += i;
        float avgInterval = sum / recentStrokeIntervals.Count;
        StrokeRate = 60f / avgInterval;
    }

    public void ResetMetrics()
    {
        strokeCount = 0;
        recentStrokeIntervals.Clear();
        StrokeRate = 0f;
        lastStrokeTime = -100f;
    }

    private float EstimateSpeedFromPower()
    {
        if (currentPower <= 0) return 0;
        return Mathf.Sqrt(currentPower / waterDrag);
    }
}