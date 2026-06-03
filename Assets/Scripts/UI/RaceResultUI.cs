using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Окно результатов гонки (UI Toolkit). Отображает статистику и предоставляет кнопку сброса.
/// Блокирует управление камерой при открытии.
/// </summary>
public class RaceResultUI : MonoBehaviour
{
    [SerializeField] private UIDocument uiDocument;
    /// <summary>Событие сброса гонки (при нажатии кнопки).</summary>
    public event System.Action OnReset;

    private Label timeLabel, distanceLabel, avgStrokeRateLabel, avgStrokeLengthLabel, avgSpeedLabel, maxSpeedLabel, totalStrokesLabel;
    private Button resetButton;

    /// <summary>Заполняет окно статистикой и активирует его.</summary>
    /// <param name="stats">Статистика заплыва.</param>
    public void SetStatistics(RaceStatistics stats)
    {
        uiDocument.gameObject.SetActive(true);
        CoachCameraController.AddUILock();
        var root = uiDocument.rootVisualElement;

        timeLabel = root.Q<Label>("TotalTime");
        distanceLabel = root.Q<Label>("Distance");
        avgStrokeRateLabel = root.Q<Label>("AvgStrokeRate");
        avgStrokeLengthLabel = root.Q<Label>("AvgStrokeLength");
        avgSpeedLabel = root.Q<Label>("AvgSpeed");
        maxSpeedLabel = root.Q<Label>("MaxSpeed");
        totalStrokesLabel = root.Q<Label>("TotalStrokes");
        resetButton = root.Q<Button>("ResetButton");

        timeLabel.text = $"Время: {stats.totalTime:F1} с";
        distanceLabel.text = $"Дистанция: {stats.distanceCovered:F1} м";
        avgStrokeRateLabel.text = $"Ср. темп: {stats.avgStrokeRate:F1} греб/мин";
        avgStrokeLengthLabel.text = $"Ср. длина гребка: {stats.avgStrokeLength:F2} м";
        avgSpeedLabel.text = $"Ср. скорость: {stats.avgSpeed:F2} м/с";
        maxSpeedLabel.text = $"Макс. скорость: {stats.maxSpeed:F2} м/с";
        totalStrokesLabel.text = $"Всего гребков: {stats.totalStrokes}";

        resetButton.clicked += () => OnReset?.Invoke();
    }

    private void CloseWindow()
    {
        CoachCameraController.RemoveUILock();
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        CoachCameraController.RemoveUILock();
        if (resetButton != null)
            resetButton.clicked -= CloseWindow;
    }
}