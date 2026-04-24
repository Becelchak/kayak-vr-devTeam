using UnityEngine;
using System.Collections.Generic;
using System;

public class MetricsCalculator : MonoBehaviour
{
    [Header("Параметры каяка")]
    [SerializeField] private Rigidbody kayakBody;
    //[SerializeField] private float kayakMass = 100f;
    [SerializeField] private float waterDrag = 50f;     // коэффициент сопротивления воды (F = drag * v)

    [Header("Детекция гребков")]
    [SerializeField] private float powerThreshold = 5f; // порог силы для начала гребка
    [SerializeField] private float minStrokeInterval = 0.3f; // мин. интервал между гребками (сек)
    [SerializeField] private float minTimeCheckStreetrate = 1f; //мин. время для детекции темпа (сек)

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
            Debug.Log($"Темп: {StrokeRate:F1} греб/мин | Длина гребка: {StrokeLength:F2} м | Скорость: {Speed:F2} м/с");
        }
    }

    private void OnPowerChanged(float power, float dPower)
    {
        currentPower = power;
        currentDPower = dPower;

        if (power > powerThreshold && Time.time - lastStrokeTime > minStrokeInterval)
        {
            var now = Time.time;

            if(strokeCount > 0)
            {
                var interval = now - lastStrokeTime;
                AddStrokeInterval(interval);
            }

            lastStrokeTime = now;
            strokeCount++;

            strokeStartTime = now;
            strokeStartVelocity = Speed;

            //Debug.Log($"{strokeCount}");
            //UpdateStrokeRate();

            // Длину гребка можно будет вычислить по окончании гребка (когда power снова упадет ниже порога)
            // Сейчас приблизительно через приращение скорости
        }

        if (power < powerThreshold * 0.5f && strokeStartTime > 0 && Time.time - strokeStartTime > 0.2f)
        {
            float strokeDuration = Time.time - strokeStartTime;
            float deltaV = Speed - strokeStartVelocity;
            StrokeLength = (strokeStartVelocity + Speed) / 2f * strokeDuration;
            strokeStartTime = 0f;
        }

        OnMetricsUpdated?.Invoke(StrokeRate, StrokeLength, Speed);
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

    //private void UpdateStrokeRate()
    //{
    //    // Добавляем интервал с последним гребком
    //    if (strokeCount > 1)
    //    {
    //        float lastInterval = Time.time - lastStrokeTime;
    //        recentStrokeIntervals.Enqueue(lastInterval);
    //        // Храним интервалы за последние 10 секунд (или 10 гребков)
    //        while (recentStrokeIntervals.Count > 10)
    //            recentStrokeIntervals.Dequeue();

    //        float sum = 0f;
    //        foreach (var interval in recentStrokeIntervals)
    //            sum += interval;
    //        float avgInterval = sum / recentStrokeIntervals.Count;
    //        StrokeRate = 60f / avgInterval;
    //    }
    //    else
    //    {
    //        StrokeRate = 0f;
    //    }
    //}

    private float EstimateSpeedFromPower()
    {
        // Грубая оценка: F = power / velocity? Нет, power = F * v.
        // Если известна сила тяги от гребка: F = power / v (зациклено). Упростим: v = sqrt(power / drag)
        if (currentPower <= 0) return 0;
        return Mathf.Sqrt(currentPower / waterDrag);
    }
}