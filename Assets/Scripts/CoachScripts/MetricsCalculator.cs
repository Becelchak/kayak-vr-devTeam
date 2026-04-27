using UnityEngine;
using System.Collections.Generic;
using System;

public class MetricsCalculator : MonoBehaviour
{
    [Header("Параметры каяка")]
    [SerializeField] private Rigidbody kayakBody;
    [Tooltip("Коэффициент сопротивления воды (F = drag * v)")]
    [SerializeField] private float waterDrag = 50f;

    [Header("Детекция гребков")]
    [Tooltip("Порог силы для начала гребка")]
    [SerializeField] private float powerThreshold = 5f;
    [Tooltip("Коэффицент, показывающий выход из гребка и падения силы. Чем больше коэффицент, тем более чувстиветельна панель данных к падению силы.")]
    [SerializeField] private float powerOffCoeficent = 0.5f;
    [Tooltip("Мин. интервал между гребками (сек)")]
    [SerializeField] private float minStrokeInterval = 0.3f;
    [Tooltip("Мин. время для детекции темпа (сек)")]
    [SerializeField] private float minTimeCheckStreetrate = 1f;

    [Header("Отображение (временное)")]
    [SerializeField] private bool logToConsole = true;

    // Текущие метрики
    public float StrokeRate { get; private set; }    // темп (гребков/мин)
    public float StrokeLength { get; private set; }  // длина гребка (м)
    public float Speed { get; private set; }         // скорость (м/с)

    // Приватные поля для алгоритмов
    private float lastStrokeTime = -100f;
    private float lastStrokeRealTime = -100f;
    private int strokeCount = 0;
    private float strokeStartTime = 0f;
    private float strokeStartVelocity = 0f;
    private float currentPower = 0f;
    private float currentDPower = 0f;
    private float tempoDecayRate = 5f;
    private Queue<float> recentStrokeIntervals = new Queue<float>(); // для скользящего среднего темпа
    public event Action<float, float, float> OnMetricsUpdated;

    public event Action OnStrokeStarted;
    public event Action OnStrokeEnded;

    private bool isStrokeInProgress = false;

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
            //Debug.Log($"Темп: {StrokeRate:F1} греб/мин | Длина гребка: {StrokeLength:F2} м | Скорость: {Speed:F2} м/с");
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
                //StrokeLength = (strokeStartVelocity + Speed) / 2f * strokeDuration;
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
        Debug.Log($"Длина гребка: {length:F2} м");
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
        // Грубая оценка: F = power / velocity? Нет, power = F * v.
        // Если известна сила тяги от гребка: F = power / v (зациклено). Упростим: v = sqrt(power / drag)
        if (currentPower <= 0) return 0;
        return Mathf.Sqrt(currentPower / waterDrag);
    }
}