using UnityEngine;
using System;
using System.Collections.Generic;

public class RaceService : BaseService, IRaceService
{
    [Header("Race Points")]
    [SerializeField] private Transform startPoint;
    [SerializeField] private Transform finishPoint;

    [Header("UI References")]
    [SerializeField] private GameObject raceResultUIPrefab;

    [Header("MiniMap Line")]
    [SerializeField] private LineRenderer raceLine;
    [SerializeField] private Material lineMaterial;

    public event Action<RaceStatistics> OnRaceFinished;

    private RaceStatistics currentStats;
    private bool isRaceActive = false;
    private float raceStartTime;
    private float currentStrokeCount = 0f;
    private List<float> strokeRates = new List<float>();
    private List<float> strokeLengths = new List<float>();
    private List<float> speeds = new List<float>();

    private GameObject currentResultWindow;
    private Rigidbody kayakRigidbody;

    private void Start()
    {
        // Находим ригидбоди каяка (тег "Player")
        var kayak = GameObject.FindGameObjectWithTag("Player");
        if (kayak != null) kayakRigidbody = kayak.GetComponent<Rigidbody>();

        // Настройка LineRenderer (если не назначен в инспекторе, создаём)
        if (raceLine == null)
        {
            GameObject lineObj = new GameObject("RaceLine");
            raceLine = lineObj.AddComponent<LineRenderer>();
            raceLine.transform.SetParent(transform);
        }
        raceLine.positionCount = 2;
        raceLine.SetPosition(0, startPoint.position);
        raceLine.SetPosition(1, finishPoint.position);
        raceLine.startWidth = 5f;
        raceLine.endWidth = 5f;
        //raceLine.material = lineMaterial;
        raceLine.enabled = true; // линия видна всегда (можно управлять)
    }

    public void StartRace()
    {
        if (isRaceActive) return;

        currentStats = new RaceStatistics();
        isRaceActive = true;
        raceStartTime = Time.time;

        strokeRates.Clear();
        strokeLengths.Clear();
        speeds.Clear();
        currentStrokeCount = 0;

        var metrics = FindObjectOfType<MetricsCalculator>();
        if (metrics != null)
        {
            metrics.OnMetricsUpdated += OnMetricsUpdated;
            metrics.OnStrokeEnded += OnStrokeEnded;
        }

        Debug.Log("Race started!");
    }

    private void OnMetricsUpdated(float strokeRate, float strokeLength, float speed)
    {
        if (!isRaceActive) return;
        strokeRates.Add(strokeRate);
        strokeLengths.Add(strokeLength);
        speeds.Add(speed);
    }

    private void OnStrokeEnded()
    {
        if (!isRaceActive) return;
        currentStrokeCount++;
    }

    public void FinishRace()
    {
        if (!isRaceActive) return;

        isRaceActive = false;
        currentStats.totalTime = Time.time - raceStartTime;
        currentStats.distanceCovered = Vector3.Distance(startPoint.position, finishPoint.position);
        currentStats.totalStrokes = Mathf.RoundToInt(currentStrokeCount);
        currentStats.avgStrokeRate = (strokeRates.Count > 0) ? Average(strokeRates) : 0;
        currentStats.avgStrokeLength = (strokeLengths.Count > 0) ? Average(strokeLengths) : 0;
        currentStats.avgSpeed = (speeds.Count > 0) ? Average(speeds) : 0;
        currentStats.maxSpeed = (speeds.Count > 0) ? Max(speeds) : 0;

        var metrics = FindObjectOfType<MetricsCalculator>();
        if (metrics != null)
        {
            metrics.OnMetricsUpdated -= OnMetricsUpdated;
            metrics.OnStrokeEnded -= OnStrokeEnded;
        }

        OnRaceFinished?.Invoke(currentStats);
        ShowRaceResults();
    }

    private void ShowRaceResults()
    {
        if (raceResultUIPrefab != null && currentResultWindow == null)
        {
            currentResultWindow = Instantiate(raceResultUIPrefab);
            var ui = currentResultWindow.GetComponent<RaceResultUI>();
            if (ui != null)
            {
                ui.SetStatistics(currentStats);
                ui.OnReset += ResetRace;
            }
        }
    }

    public void ResetRace()
    {
        if (currentResultWindow != null)
        {
            Destroy(currentResultWindow);
            currentResultWindow = null;
        }

        TeleportPlayerToStart();

        isRaceActive = false;
        strokeRates.Clear();
        strokeLengths.Clear();
        speeds.Clear();
        currentStrokeCount = 0;

        Debug.Log("Race reset. Ready for new race.");
    }

    private void TeleportPlayerToStart()
    {
        var kayak = GameObject.FindGameObjectWithTag("Player");
        if (kayak != null)
        {
            kayak.transform.position = startPoint.position;
            if (kayakRigidbody != null)
            {
                kayakRigidbody.linearVelocity = Vector3.zero;
                kayakRigidbody.angularVelocity = Vector3.zero;
            }
        }
    }

    private Vector3 GetKayakPosition()
    {
        var kayak = GameObject.FindGameObjectWithTag("Player");
        return kayak != null ? kayak.transform.position : Vector3.zero;
    }

    //void Update()
    //{
    //    if (!isRaceActive) return;
    //    if (Vector3.Distance(GetKayakPosition(), finishPoint.position) < 2f)
    //    {
    //        FinishRace();
    //    }
    //}

    private float Average(List<float> list)
    {
        if (list.Count == 0) return 0;
        float sum = 0;
        foreach (var v in list) sum += v;
        return sum / list.Count;
    }

    private float Max(List<float> list)
    {
        float max = -1;
        foreach (var v in list) if (v > max) max = v;
        return max;
    }

    protected override Type GetServiceType() => typeof(IRaceService);
}