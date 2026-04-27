using UnityEngine;

public class StrokeLengthMeasurer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody kayakRb;
    [SerializeField] private MetricsCalculator metrics;

    [Header("Settings")]
    [Tooltip("Если false – использовать весло")]
    [SerializeField] private bool useBoatVelocity = true; 
    [SerializeField] private Transform leftBladeTip;
    [SerializeField] private Transform rightBladeTip;

    private float currentStrokeLength = 0f;
    private bool isStroking = false;
    private Vector3 lastBoatPos;
    private Vector3 lastStrokePosition;

    private void OnEnable()
    {
        if (metrics != null)
        {
            metrics.OnStrokeStarted += StartStroke;
            metrics.OnStrokeEnded += EndStroke;
        }
        DoublePaddleSystem.OnLeftBladeEnterWater += OnBladeEnterWater;
        DoublePaddleSystem.OnRightBladeEnterWater += OnBladeEnterWater;
        DoublePaddleSystem.OnLeftBladeExitWater += OnBladeExitWater;
        DoublePaddleSystem.OnRightBladeExitWater += OnBladeExitWater;

    }

    private void StartStroke()
    {
        Debug.Log("Start Stroke");
        isStroking = true;
        currentStrokeLength = 0f;
        if (kayakRb != null) lastBoatPos = kayakRb.position;
    }

    private void EndStroke()
    {
        Debug.Log("End Stroke");
        isStroking = false;
        metrics?.SetStrokeLength(currentStrokeLength);
    }

    private void FixedUpdate()
    {
        if (!isStroking) return;

        if (useBoatVelocity && kayakRb != null)
        {
            Vector3 currentPos = kayakRb.position;
            float delta = Vector3.Distance(currentPos, lastBoatPos);
            currentStrokeLength += delta;
            lastBoatPos = currentPos;
        }
        else
        {
            // Альтернатива: измерять перемещение лопасти (например, активной в данный момент)
            // Нужно определить, какая лопасть сейчас в воде (левая или правая)
        }
    }

    private void OnBladeEnterWater()
    {
        // Начало гребка (любая лопасть вошла в воду)
        if (!isStroking)
            StartStroke();
        lastStrokePosition = kayakRb.position;
    }

    private void OnBladeExitWater()
    {
        // Окончание гребка (лопасть вышла из воды)
        float travelled = Vector3.Distance(kayakRb.position, lastStrokePosition);
        currentStrokeLength += travelled;
        EndStroke();
    }
}