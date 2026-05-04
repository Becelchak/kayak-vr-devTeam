using UnityEngine;
using System.Collections.Generic;
using System;

public class MetricsCalculator : MonoBehaviour
{
    [Header("��������� �����")]
    [SerializeField] private Rigidbody kayakBody;
    [Tooltip("����������� ������������� ���� (F = drag * v)")]
    [SerializeField] private float waterDrag = 50f;

    [Header("�������� �������")]
    [Tooltip("����� ���� ��� ������ ������")]
    [SerializeField] private float powerThreshold = 5f;
    [Tooltip("����������, ������������ ����� �� ������ � ������� ����. ��� ������ ����������, ��� ����� �������������� ������ ������ � ������� ����.")]
    [SerializeField] private float powerOffCoeficent = 0.5f;
    [Tooltip("���. �������� ����� �������� (���)")]
    [SerializeField] private float minStrokeInterval = 0.3f;
    [Tooltip("���. ����� ��� �������� ����� (���)")]
    [SerializeField] private float minTimeCheckStreetrate = 1f;

    [Header("����������� (���������)")]
    [SerializeField] private bool logToConsole = true;

    // ������� �������
    public float StrokeRate { get; private set; }    // ���� (�������/���)
    public float StrokeLength { get; private set; }  // ����� ������ (�)
    public float Speed { get; private set; }         // �������� (�/�)

    // ��������� ���� ��� ����������
    private float lastStrokeTime = -100f;
    private float lastStrokeRealTime = -100f;
    private int strokeCount = 0;
    private float strokeStartTime = 0f;
    private float strokeStartVelocity = 0f;
    private float currentPower = 0f;
    private float currentDPower = 0f;
    private float tempoDecayRate = 5f;
    private Queue<float> recentStrokeIntervals = new Queue<float>(); // ��� ����������� �������� �����
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
            //Debug.Log($"����: {StrokeRate:F1} ����/��� | ����� ������: {StrokeLength:F2} � | ��������: {Speed:F2} �/�");
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
        // Debug.Log($"����� ������: {length:F2} �");
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
        // ������ ������: F = power / velocity? ���, power = F * v.
        // ���� �������� ���� ���� �� ������: F = power / v (���������). ��������: v = sqrt(power / drag)
        if (currentPower <= 0) return 0;
        return Mathf.Sqrt(currentPower / waterDrag);
    }
}