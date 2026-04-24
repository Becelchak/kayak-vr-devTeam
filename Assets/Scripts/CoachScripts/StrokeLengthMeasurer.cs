using UnityEngine;

public class StrokeLengthMeasurer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody kayakRb;          // каяк
    [SerializeField] private MetricsCalculator metrics;   // чтобы знать момент начала/конца гребка

    [Header("Settings")]
    [SerializeField] private bool useBoatVelocity = true; // если false – использовать весло
    [SerializeField] private Transform leftBladeTip;      // опционально для метода весла
    [SerializeField] private Transform rightBladeTip;

    private float currentStrokeLength = 0f;
    private bool isStroking = false;
    private Vector3 lastBoatPos;

    private void OnEnable()
    {
        if (metrics != null)
        {
            metrics.OnStrokeStarted += StartStroke;
            metrics.OnStrokeEnded += EndStroke;
        }
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
            // Интегрируем скорость каяка за время гребка
            Vector3 currentPos = kayakRb.position;
            float delta = Vector3.Distance(currentPos, lastBoatPos);
            currentStrokeLength += delta;
            lastBoatPos = currentPos;
        }
        else
        {
            // Альтернатива: измерять перемещение лопасти (например, активной в данный момент)
            // Нужно определить, какая лопасть сейчас в воде (левая или правая)
            // Для простоты – не реализую здесь, но идея ясна.
        }
    }
}