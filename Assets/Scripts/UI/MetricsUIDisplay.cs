using UnityEngine;
using TMPro;

public class MetricsUIDisplay : MonoBehaviour
{
    [Header("UI Текстовые элементы (один текст на метрику)")]
    [SerializeField] private TextMeshProUGUI tempoText;      // будет "X.X греб/мин"
    [SerializeField] private TextMeshProUGUI strokeLengthText; // будет "X.XX м"
    [SerializeField] private TextMeshProUGUI speedText;       // будет "X.XX м/с"

    [Header("Настройки форматирования")]
    [SerializeField] private string tempoFormat = "F1";
    [SerializeField] private string strokeLengthFormat = "F2";
    [SerializeField] private string speedFormat = "F2";

    [Header("Цвета индикации")]
    [SerializeField] private Color increaseColor = Color.green;
    [SerializeField] private Color decreaseColor = Color.red;
    [SerializeField] private Color neutralColor = Color.white;

    [Header("Длительность подсветки (сек)")]
    [SerializeField] private float highlightDuration = 0.3f; // сколько секунд горит цвет, потом возврат к нейтральному

    private MetricsCalculator metricsCalculator;
    private float lastUpdateTime;
    private const float UPDATE_INTERVAL = 0.1f;

    // Предыдущие значения
    private float lastTempo = -1f;
    private float lastStrokeLength = -1f;
    private float lastSpeed = -1f;

    // Таймеры для возврата цвета к нейтральному
    private float tempoHighlightTimer = 0f;
    private float strokeHighlightTimer = 0f;
    private float speedHighlightTimer = 0f;

    // Текущие направления изменения (для корутин или таймеров)
    private bool tempoIncreased = false;
    private bool strokeIncreased = false;
    private bool speedIncreased = false;

    private void Start()
    {
        metricsCalculator = FindObjectOfType<MetricsCalculator>();
        if (metricsCalculator == null)
        {
            Debug.LogError("MetricsCalculator не найден!");
            return;
        }

        // Инициализация цветов нейтральным
        SetNeutralColors();

        // Первоначальное обновление
        UpdateAllMetrics();
    }

    private void Update()
    {
        if (metricsCalculator == null) return;

        // Обновляем UI с заданной частотой
        if (Time.time - lastUpdateTime >= UPDATE_INTERVAL)
        {
            lastUpdateTime = Time.time;
            UpdateAllMetrics();
        }

        // Уменьшаем таймеры и возвращаем цвета к нейтральному, если время вышло
        UpdateHighlightTimers();
    }

    private void UpdateAllMetrics()
    {
        float currentTempo = metricsCalculator.StrokeRate;
        float currentStrokeLength = metricsCalculator.StrokeLength;
        float currentSpeed = metricsCalculator.Speed;

        // Обновляем текст с единицами измерения
        if (tempoText != null)
            tempoText.text = $"{currentTempo.ToString(tempoFormat)} сек";
        if (strokeLengthText != null)
            strokeLengthText.text = $"{currentStrokeLength.ToString(strokeLengthFormat)} м";
        if (speedText != null)
            speedText.text = $"{currentSpeed.ToString(speedFormat)} м/с";

        // Обрабатываем цветовые индикации
        HandleColorChange(ref lastTempo, currentTempo, tempoText, ref tempoHighlightTimer, ref tempoIncreased);
        HandleColorChange(ref lastStrokeLength, currentStrokeLength, strokeLengthText, ref strokeHighlightTimer, ref strokeIncreased);
        HandleColorChange(ref lastSpeed, currentSpeed, speedText, ref speedHighlightTimer, ref speedIncreased);

        // Сохраняем текущие значения как предыдущие для следующего обновления
        lastTempo = currentTempo;
        lastStrokeLength = currentStrokeLength;
        lastSpeed = currentSpeed;
    }

    private void HandleColorChange(ref float lastValue, float currentValue, TextMeshProUGUI textComponent,
                                    ref float highlightTimer, ref bool increased)
    {
        if (textComponent == null) return;

        // Если нет предыдущего значения (первый кадр) - ставим нейтральный цвет
        if (lastValue < 0)
        {
            textComponent.color = neutralColor;
            return;
        }

        // Определяем направление изменения
        if (currentValue > lastValue)
        {
            // Рост - зеленый
            textComponent.color = increaseColor;
            increased = true;
            highlightTimer = highlightDuration;
        }
        else if (currentValue < lastValue)
        {
            // Падение - красный
            textComponent.color = decreaseColor;
            increased = false;
            highlightTimer = highlightDuration;
        }
        else
        {
            // Если значение не изменилось, но таймер ещё активен - оставляем текущий цвет
            if (highlightTimer <= 0f)
            {
                textComponent.color = neutralColor;
            }
        }
    }

    private void UpdateHighlightTimers()
    {
        if (tempoHighlightTimer > 0f)
        {
            tempoHighlightTimer -= Time.deltaTime;
            if (tempoHighlightTimer <= 0f && tempoText != null)
                tempoText.color = neutralColor;
        }

        if (strokeHighlightTimer > 0f)
        {
            strokeHighlightTimer -= Time.deltaTime;
            if (strokeHighlightTimer <= 0f && strokeLengthText != null)
                strokeLengthText.color = neutralColor;
        }

        if (speedHighlightTimer > 0f)
        {
            speedHighlightTimer -= Time.deltaTime;
            if (speedHighlightTimer <= 0f && speedText != null)
                speedText.color = neutralColor;
        }
    }

    private void SetNeutralColors()
    {
        if (tempoText != null) tempoText.color = neutralColor;
        if (strokeLengthText != null) strokeLengthText.color = neutralColor;
        if (speedText != null) speedText.color = neutralColor;
    }

    public void ForceUpdate() => UpdateAllMetrics();
}