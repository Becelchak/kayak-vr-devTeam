using UnityEngine;
using UnityEngine.UIElements;

public class StatisticsModalController : MonoBehaviour
{
    private UIDocument uiDocument;
    public event System.Action OnReset;
    private bool isVisible = false;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        //if (uiDocument != null)
        //uiDocument.enabled = false;
    }

    private void OnEnable()
    {
        // Отложенная инициализация
        //if (uiDocument != null && uiDocument.rootVisualElement != null)
        //{
        //    var reseteBtn = uiDocument.rootVisualElement.Q<Button>("ResetButton");
        //    if (reseteBtn != null)
        //        reseteBtn.clicked += () => OnReset?.Invoke();
        //    //reseteBtn.RegisterCallback<ClickEvent>(_ => Hide());
        //}
    }

    public void Initialized()
    {
        gameObject.SetActive(false);
    }

    public void Show(RaceStatistics stats = null)
    {
        gameObject.SetActive(true);
        if (uiDocument == null) return;

        if (uiDocument != null && uiDocument.rootVisualElement != null)
        {
            var reseteBtn = uiDocument.rootVisualElement.Q<Button>("ResetButton");
            if (reseteBtn != null)
            {
                reseteBtn.clicked += () => { Debug.Log("Reset clicked"); OnReset?.Invoke(); };
                Debug.Log("ЗАРЕГЕСТРИРОВАН КЛИК");
            }
        }

        // ===== РЕАЛЬНЫЕ ДАННЫЕ =====
        if (stats != null)
        {
            UpdateStatistics(stats);
        }
        // ===== ТЕСТОВЫЕ ДАННЫЕ =====
        else
        {
            UpdateStatistics(GetTestStatistics());
        }

        //uiDocument.enabled = true;
        isVisible = true;
    }

    // Тестовые данные для демонстрации
    private RaceStatistics GetTestStatistics()
    {
        return new RaceStatistics
        {
            totalTime = 143.5f,
            distanceCovered = 850f,
            avgStrokeRate = 52.3f,
            avgStrokeLength = 1.35f,
            avgSpeed = 3.8f,
            maxSpeed = 5.2f,
            totalStrokes = 42
        };
    }

    public void Hide()
    {
        if (uiDocument == null) return;

        //uiDocument.enabled = false;
        gameObject.SetActive(false);
        isVisible = false;
        Debug.Log("Statistics modal hidden");
    }

    public void Toggle(RaceStatistics stats = null)
    {
        if (isVisible)
            Hide();
        else
            Show(stats);
    }

    private void UpdateStatistics(RaceStatistics stats)
    {
        if (stats == null || uiDocument == null || uiDocument.rootVisualElement == null) return;

        var root = uiDocument.rootVisualElement;

        SetLabelValue(root, "TotalTimeValue", $"{stats.totalTime:F1} сек");
        SetLabelValue(root, "DistanceValue", $"{stats.distanceCovered:F1} м");
        SetLabelValue(root, "AvgStrokeRateValue", $"{stats.avgStrokeRate:F1} греб/мин");
        SetLabelValue(root, "AvgStrokeLengthValue", $"{stats.avgStrokeLength:F1} м");
        SetLabelValue(root, "AvgSpeedValue", $"{stats.avgSpeed:F1} м/с");
        SetLabelValue(root, "MaxSpeedValue", $"{stats.maxSpeed:F1} м/с");
        SetLabelValue(root, "TotalStrokesValue", $"{stats.totalStrokes}");
    }

    private void SetLabelValue(VisualElement root, string name, string value)
    {
        var label = root.Q<Label>(name);
        if (label != null)
            label.text = value;
    }

    public bool IsVisible => isVisible;
}