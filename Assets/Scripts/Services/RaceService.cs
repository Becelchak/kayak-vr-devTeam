using UnityEngine;
using System;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// Управление трассой: старт/финиш, сбор статистики, отображение линии маршрута.
/// </summary>
public class RaceService : BaseService, IRaceService
{
    [Header("Race Points")]
    [Tooltip("Точка старта (коллайдер-триггер)")]
    [SerializeField] private Transform startPoint;
    [Tooltip("Точка финиша (коллайдер-триггер)")]
    [SerializeField] private Transform finishPoint;

    //[Header("UI References")]
    //[Tooltip("Префаб окна статистики (UIDocument)")]
    //[SerializeField] private GameObject raceResultUIPrefab;

    [Header("MiniMap Line")]
    [Tooltip("LineRenderer для отображения маршрута на миникарте")]
    [SerializeField] private LineRenderer raceLine;
    [Tooltip("Материал линии маршрута")]
    [SerializeField] private Material lineMaterial;

    /// <summary>Событие, вызываемое при завершении гонки. Передаёт статистику.</summary>
    public event Action<RaceStatistics> OnRaceFinished;

    private RaceStatistics currentStats;
    private bool isRaceActive = false;
    public bool IsRaceActive => isRaceActive;
    private float raceStartTime;
    private float currentStrokeCount = 0f;
    private List<float> strokeRates = new List<float>();
    private List<float> strokeLengths = new List<float>();
    private List<float> speeds = new List<float>();

    //private GameObject currentResultWindow;
    private Rigidbody kayakRigidbody;
    [Tooltip("Родительский объект для каяка и всех его компонентов")]
    [SerializeField]private GameObject kayakParentObject;

    private List<RaceStatistics> completedRaces = new List<RaceStatistics>();

    private void Start()
    {
        if (kayakParentObject != null) kayakRigidbody = kayakParentObject.GetComponent<Rigidbody>();

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
        raceLine.enabled = true;

        currentStats = new RaceStatistics();
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

        //Debug.Log("Race started!");
    }

    private void OnMetricsUpdated(float strokeRate, float strokeLength, float speed)
    {
        if (!isRaceActive) return;
        strokeRates.Add(strokeRate);
        strokeLengths.Add(strokeLength);
        speeds.Add(speed);

        //currentStats.totalTime = Time.time - raceStartTime;
        //currentStats.distanceCovered = Vector3.Distance(startPoint.position, finishPoint.position);
        //currentStats.totalStrokes = Mathf.RoundToInt(currentStrokeCount);
        //currentStats.avgStrokeRate = (strokeRates.Count > 0) ? Average(strokeRates) : 0;
        //currentStats.avgStrokeLength = (strokeLengths.Count > 0) ? Average(strokeLengths) : 0;
        //currentStats.avgSpeed = (speeds.Count > 0) ? Average(speeds) : 0;
        //currentStats.maxSpeed = (speeds.Count > 0) ? Max(speeds) : 0;
    }

    public List<RaceStatistics> GetAllRaces()
    {
        return completedRaces;
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

        // Сохраняем в историю
        completedRaces.Add(currentStats);

        OnRaceFinished?.Invoke(currentStats);
        //ShowRaceResults();
    }

    //private void ShowRaceResults()
    //{
    //    if (raceResultUIPrefab != null && currentResultWindow == null)
    //    {
    //        currentResultWindow = Instantiate(raceResultUIPrefab);
    //        var modal = currentResultWindow.GetComponent<StatisticsModalController>();
    //        if (modal != null)
    //        {
    //            //modal.SetStatistics(currentStats);
    //            Debug.Log("ВПЕРЕД");
    //            modal.Show(currentStats);
    //            modal.OnReset += ResetRace;
    //        }
    //    }
    //}

    public void ResetRace()
    {
        // Отключение физических компонентов, дабы исключить их влияние на перемещение
        if (kayakParentObject != null)
        {
            var paddle = kayakParentObject.GetComponent<DoublePaddleSystem>();
            if (paddle != null) paddle.enabled = false;

            var buoyancy = kayakParentObject.GetComponent<BuoyancySystem>();
            if (buoyancy != null) buoyancy.enabled = false;
        }

        TeleportPlayerToStart();
        StartCoroutine(EnablePhysicsAfterFrame());

        isRaceActive = false;
        strokeRates.Clear();
        strokeLengths.Clear();
        speeds.Clear();
        currentStrokeCount = 0;

        Debug.Log("Race reset. Ready for new race.");
    }

    private void TeleportPlayerToStart()
    {
        if (kayakParentObject != null)
        {
            kayakParentObject.transform.position = startPoint.position;
            if (kayakRigidbody != null)
            {
                kayakRigidbody.position = startPoint.position;
                kayakRigidbody.rotation = startPoint.rotation;
                kayakRigidbody.linearVelocity = Vector3.zero;
                kayakRigidbody.angularVelocity = Vector3.zero;
            }
            else
            {

                kayakParentObject.transform.position = startPoint.position;
            }
        }
    }

    private IEnumerator EnablePhysicsAfterFrame()
    {
        yield return new WaitForFixedUpdate(); // ждём один физический кадр

        if (kayakParentObject != null)
        {
            var paddle = kayakParentObject.GetComponent<DoublePaddleSystem>();
            if (paddle != null) paddle.enabled = true;

            var buoyancy = kayakParentObject.GetComponent<BuoyancySystem>();
            if (buoyancy != null) buoyancy.enabled = true;
        }
    }

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
    public RaceStatistics GetCurrentStatistics()
    {
        //if (currentStats == null)
        //{
        //    return new RaceStatistics(); // пустая статистика, если гонка не завершена
        //}
        return currentStats;
    }

    protected override Type GetServiceType() => typeof(IRaceService);
}