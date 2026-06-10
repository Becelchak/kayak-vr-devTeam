using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using Cursor = UnityEngine.Cursor;

public class UIToolkitManager : MonoBehaviour
{
    [Header("UIDocuments")]
    [SerializeField] private UIDocument routeScreenDocument;
    [SerializeField] private UIDocument dashboardDocument;

    [Header("Data Providers")]
    [SerializeField] private IRatingDataProvider ratingDataProvider;
    [SerializeField] private ICarouselDataProvider carouselDataProvider;

    [Header("Metrics Calculator")]
    [SerializeField] private MetricsCalculator metricsCalculator;

    [Header("Game Systems")]
    [SerializeField] private GameObject athleteSystem;
    [SerializeField] private GameObject coachSystem;

    [Header("Statistics Modal")]
    [SerializeField] private StatisticsModalController statisticsModal;

    // Элементы дашборда
    private Label tempoValue;
    private Label strokeValue;
    private Label speedValue;
    private Label tempoArrow;
    private Label strokeArrow;
    private Label speedArrow;

    // Предыдущие значения для расчета тренда
    private float previousStrokeRate;
    private float previousStrokeLength;
    private float previousSpeed;

    // Стандартные значения для сравнения
    private float standardTempo = 4.1f;
    private float standardStrokeLength = 130f;
    private float standardSpeed = 3.8f;

    // Элементы таблицы
    private VisualElement statsList;
    private Button fullTableButton;

    // Элементы карусели
    private int currentRouteIndex;
    private List<RouteData> routes;

    // График
    private GraphController graphController;
    private int currentGraphType = 0;

    private bool isAthleteMode;

    private void Start()
    {
        isAthleteMode = false;//Display.displays.Length <= 1;
        Debug.Log($"Mode: {(isAthleteMode ? "ATHLETE (VR)" : "COACH (PC)")}");

        SetupDashboard();
        SetupRatingTable();
        SetupCarousel();
        SetupButtons();
        SetupCursor();
        SetupGraph();
        SetupWeatherButtons();
        SetupStatisticsModal();
        SetupGameStart();

        if (metricsCalculator != null)
            metricsCalculator.OnMetricsUpdated += UpdateDashboard;

        var raceService = ServiceLocator.Instance.GetService<IRaceService>();
        if (raceService != null)
            raceService.OnRaceFinished += OnRaceFinished;
    }

    private void SetupDashboard()
    {
        if (dashboardDocument == null)
        {
            Debug.LogError("❌ dashboardDocument is NULL!");
            return;
        }

        Debug.Log($"Dashboard active: {dashboardDocument.gameObject.activeInHierarchy}");

        var root = dashboardDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("❌ Dashboard root is NULL!");
            return;
        }

        root.style.display = DisplayStyle.Flex;

        tempoValue = root.Q<Label>("TempoValue");
        strokeValue = root.Q<Label>("StrokeValue");
        speedValue = root.Q<Label>("SpeedValue");
        tempoArrow = root.Q<Label>("TempoArrow");
        strokeArrow = root.Q<Label>("StrokeArrow");
        speedArrow = root.Q<Label>("SpeedArrow");

        Debug.Log($"TempoValue found: {tempoValue != null}");
        Debug.Log($"StrokeValue found: {strokeValue != null}");
        Debug.Log($"SpeedValue found: {speedValue != null}");

        if (tempoValue != null) tempoValue.text = "0.0с";
        if (strokeValue != null) strokeValue.text = "0";
        if (speedValue != null) speedValue.text = "0.0м/с";

        previousStrokeRate = standardTempo;
        previousStrokeLength = standardStrokeLength;
        previousSpeed = standardSpeed;
    }

    private void SetupRatingTable()
    {
        if (routeScreenDocument == null) return;
        var root = routeScreenDocument.rootVisualElement;
        statsList = root.Q<VisualElement>("StatsList");
        fullTableButton = root.Q<Button>("FullTableButton");

        if (fullTableButton != null)
            fullTableButton.RegisterCallback<ClickEvent>(_ => OnFullTableClick());

        LoadRatingData();
    }

    private void SetupCarousel()
    {
        if (carouselDataProvider != null)
        {
            routes = carouselDataProvider.GetRoutes();
            if (routes != null && routes.Count > 0)
                UpdateCarousel(0);
        }
        else
        {
            routes = GetTestRoutes();
            UpdateCarousel(0);
        }
    }

    private void SetupButtons()
    {
        if (routeScreenDocument == null) return;
        var root = routeScreenDocument.rootVisualElement;

        root.Q<Button>("LeftArrow")?.RegisterCallback<ClickEvent>(_ => OnLeftArrowClick());
        root.Q<Button>("RightArrow")?.RegisterCallback<ClickEvent>(_ => OnRightArrowClick());
        root.Q<Button>("StartTrainingButton")?.RegisterCallback<ClickEvent>(_ => OnStartTraining());
        root.Q<Button>("ChartLeftArrow")?.RegisterCallback<ClickEvent>(_ => OnChartLeftClick());
        root.Q<Button>("ChartRightArrow")?.RegisterCallback<ClickEvent>(_ => OnChartRightClick());
    }

    private void SetupCursor()
    {
        if (isAthleteMode)
        {
            // VR режим — курсор скрыт
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            // ПК режим — курсор видим
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    private void SetupGraph()
    {
        if (routeScreenDocument == null)
        {
            Debug.LogWarning("RouteScreenDocument is null, graph not setup");
            return;
        }

        var root = routeScreenDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogWarning("RouteScreen root is null, graph not setup");
            return;
        }

        var chartArea = root.Q<VisualElement>("ChartArea");
        if (chartArea == null)
        {
            Debug.LogWarning("ChartArea not found in UXML!");
            return;
        }

        graphController = new GraphController(chartArea);

        // ===== ТЕСТОВЫЕ ДАННЫЕ (активны) =====
        graphController.LoadTestData("speed");

        // ===== РЕАЛЬНЫЕ ДАННЫЕ (закомментированы) =====
        // Раскомментировать для работы с реальными данными из RaceService
        /*
        var raceService = ServiceLocator.Instance.GetService<IRaceService>();
        if (raceService != null)
        {
            var raceData = ConvertRaceToGraphData(raceService.GetCurrentStatistics());
            graphController.LoadRealData(raceData, "speed");
        }
        */

        graphController.OnPrevPeriod += () => OnGraphPrevPeriod();
        graphController.OnNextPeriod += () => OnGraphNextPeriod();

        Debug.Log("Graph setup complete");
    }

    // ===== ДЛЯ РЕАЛЬНЫХ ДАННЫХ (закомментировано) =====
    /*
    private List<(float time, float value)> ConvertRaceToGraphData(RaceStatistics stats)
    {
        if (stats == null) return new List<(float, float)>();

        // Здесь должна быть конвертация данных заезда в точки графика
        var data = new List<(float, float)>();
        // TODO: добавить логику конвертации
        return data;
    }
    */

    private void OnGraphPrevPeriod()
    {
        currentGraphType--;
        if (currentGraphType < 0) currentGraphType = 2;
        UpdateGraphByType();
        Debug.Log($"Previous period clicked, switch to: {GetGraphName()}");
    }

    private void OnGraphNextPeriod()
    {
        currentGraphType++;
        if (currentGraphType > 2) currentGraphType = 0;
        UpdateGraphByType();
        Debug.Log($"Next period clicked, switch to: {GetGraphName()}");
    }

    private void SetupWeatherButtons()
    {
        if (dashboardDocument == null) return;
        var root = dashboardDocument.rootVisualElement;

        var weatherContainer = root.Q<VisualElement>("right-panel"); // контейнер с кнопками
        var sunnyBtn = weatherContainer?.Q<Button>("SunnyButton");
        var rainyBtn = weatherContainer?.Q<Button>("RainyButton");

        if (sunnyBtn == null || rainyBtn == null)
        {
            Debug.LogWarning("Weather buttons not found!");
            return;
        }

        var weatherService = ServiceLocator.Instance.GetService<IWeatherService>();
        if (weatherService == null)
        {
            Debug.LogWarning("WeatherService not found!");
            return;
        }

        // Устанавливаем начальное состояние
        UpdateWeatherUI(weatherService.IsRaining, sunnyBtn, rainyBtn, weatherContainer);

        sunnyBtn.RegisterCallback<ClickEvent>(_ => {
            weatherService.SetRain(false);
            UpdateWeatherUI(false, sunnyBtn, rainyBtn, weatherContainer);
        });

        rainyBtn.RegisterCallback<ClickEvent>(_ => {
            weatherService.SetRain(true);
            UpdateWeatherUI(true, sunnyBtn, rainyBtn, weatherContainer);
        });
    }

    private void UpdateWeatherUI(bool isRaining, Button sunnyBtn, Button rainyBtn, VisualElement weatherContainer)
    {
        if (isRaining)
        {
            sunnyBtn?.RemoveFromClassList("active-sunny");
            rainyBtn?.AddToClassList("active-rainy");
            weatherContainer?.RemoveFromClassList("weather-sunny");
            weatherContainer?.AddToClassList("weather-rainy");
        }
        else
        {
            sunnyBtn?.AddToClassList("active-sunny");
            rainyBtn?.RemoveFromClassList("active-rainy");
            weatherContainer?.RemoveFromClassList("weather-rainy");
            weatherContainer?.AddToClassList("weather-sunny");
        }
    }

    private void SetupStatisticsModal()
    {
        if (dashboardDocument == null) return;
        var root = dashboardDocument.rootVisualElement;
        var settingsBtn = root.Q<Button>("SettingsButton");

        if (settingsBtn != null && statisticsModal != null)
        {
            settingsBtn.RegisterCallback<ClickEvent>(_ => {
                if (statisticsModal.IsVisible)
                {
                    statisticsModal.Hide();
                    Debug.Log("Statistics modal closed");
                }
                else
                {
                    var raceService = ServiceLocator.Instance.GetService<IRaceService>();
                    if (raceService != null)
                    {
                        var stats = raceService.GetCurrentStatistics();
                        statisticsModal.Show(stats);
                    }
                    else
                    {
                        statisticsModal.Show();
                    }
                    Debug.Log("Statistics modal opened");
                }
            });
        }
    }

    private void SetupGameStart()
    {
        var raceService = ServiceLocator.Instance.GetService<IRaceService>();
        if (raceService != null)
        {
            raceService.OnRaceFinished += OnRaceFinished;
        }
    }

    private void OnRaceFinished(RaceStatistics stats)
    {
        Debug.Log($"Race finished! Time: {stats.totalTime:F1} sec");
        statisticsModal?.Show(stats);
    }

    private void UpdateDashboard(float strokeRate, float strokeLength, float speed)
    {
        if (tempoValue != null) tempoValue.text = $"{strokeRate:F1}с";
        if (strokeValue != null) strokeValue.text = $"{strokeLength:F0}";
        if (speedValue != null) speedValue.text = $"{speed:F1}м/с";

        UpdateTempoMetric(strokeRate);
        UpdateStrokeMetric(strokeLength);
        UpdateSpeedMetric(speed);

        previousStrokeRate = strokeRate;
        previousStrokeLength = strokeLength;
        previousSpeed = speed;
    }

    private void UpdateTempoMetric(float currentValue)
    {
        if (tempoArrow == null) return;

        // Темп: улучшение = стало меньше (зелёный ▲)
        bool isBetter = currentValue < previousStrokeRate;
        tempoArrow.text = isBetter ? "▲" : "▼";
        tempoArrow.RemoveFromClassList(isBetter ? "red-arrow" : "green-arrow");
        tempoArrow.AddToClassList(isBetter ? "green-arrow" : "red-arrow");
    }

    private void UpdateStrokeMetric(float currentValue)
    {
        if (strokeArrow == null) return;

        // Длина гребка: улучшение = стало больше (зелёный ▲)
        bool isBetter = currentValue > previousStrokeLength;
        strokeArrow.text = isBetter ? "▲" : "▼";
        strokeArrow.RemoveFromClassList(isBetter ? "red-arrow" : "green-arrow");
        strokeArrow.AddToClassList(isBetter ? "green-arrow" : "red-arrow");
    }

    private void UpdateSpeedMetric(float currentValue)
    {
        if (speedArrow == null) return;

        // Скорость: улучшение = стало больше (зелёный ▲)
        bool isBetter = currentValue > previousSpeed;
        speedArrow.text = isBetter ? "▲" : "▼";
        speedArrow.RemoveFromClassList(isBetter ? "red-arrow" : "green-arrow");
        speedArrow.AddToClassList(isBetter ? "green-arrow" : "red-arrow");
    }

    private void OnChartLeftClick()
    {
        currentGraphType--;
        if (currentGraphType < 0) currentGraphType = 2;
        Debug.Log($"Switch to graph: {GetGraphName()}");
        UpdateGraphByType();
    }

    private void OnChartRightClick()
    {
        currentGraphType++;
        if (currentGraphType > 2) currentGraphType = 0;
        Debug.Log($"Switch to graph: {GetGraphName()}");
        UpdateGraphByType();
    }

    private string GetGraphName()
    {
        switch (currentGraphType)
        {
            case 0: return "Скорость (м/с)";
            case 1: return "Темп (греб/мин)";
            case 2: return "Длина гребка (см)";
            default: return "График";
        }
    }

    private void UpdateGraphByType()
    {
        if (graphController == null) return;

        switch (currentGraphType)
        {
            case 0:
                graphController.LoadTestData("speed");
                break;
            case 1:
                graphController.LoadTestData("tempo");
                break;
            case 2:
                graphController.LoadTestData("stroke");
                break;
        }
    }

    private void LoadRatingData()
    {
        // ===== РЕАЛЬНЫЕ ДАННЫЕ (раскомментировать для работы с RaceService) =====
        /*
        var raceService = ServiceLocator.Instance.GetService<IRaceService>();
        if (raceService != null)
        {
            var races = raceService.GetAllRaces();
            if (races != null && races.Count > 0)
            {
                var records = new List<RatingRecord>();
                for (int i = 0; i < races.Count && i < 10; i++)
                {
                    var race = races[i];
                    records.Add(new RatingRecord(
                        i + 1,
                        $"Заезд {i + 1}",
                        FormatTime(race.totalTime)
                    ));
                }
                UpdateRatingTable(records);
                return;
            }
        }
        */

        // ===== ТЕСТОВЫЕ ДАННЫЕ (активны по умолчанию) =====
        UpdateRatingTable(GetTestRatingRecords());
    }

    // Вспомогательный метод для форматирования времени (для реальных данных)
    private string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60);
        int secs = Mathf.FloorToInt(seconds % 60);
        return $"{minutes:00}:{secs:00}";
    }

    private void UpdateRatingTable(List<RatingRecord> records)
    {
        if (statsList == null) return;
        statsList.Clear();
        foreach (var record in records)
            statsList.Add(CreateRatingRow(record));
    }

    private VisualElement CreateRatingRow(RatingRecord record)
    {
        var row = new VisualElement();
        row.AddToClassList("rating-row");

        if (record.Rank == 1)
            row.AddToClassList("active-row");

        var rankLabel = new Label(record.Rank.ToString());
        rankLabel.AddToClassList("rating-rank");

        var nameLabel = new Label(record.Name);
        nameLabel.AddToClassList("rating-name");

        var timeLabel = new Label(record.Time);
        timeLabel.AddToClassList("rating-time");

        row.Add(rankLabel);
        row.Add(nameLabel);
        row.Add(timeLabel);

        return row;
    }

    private void OnFullTableClick() => Debug.Log("Full table button clicked");
    private void OnLeftArrowClick() => Debug.Log("Left arrow clicked");
    private void OnRightArrowClick() => Debug.Log("Right arrow clicked");

    private void OnStartTraining()
    {
        Debug.Log("Start training button clicked");

        // Скрываем RouteScreen
        if (routeScreenDocument != null)
            routeScreenDocument.rootVisualElement.style.display = DisplayStyle.None;

        // Запускаем гонку
        var raceService = ServiceLocator.Instance.GetService<IRaceService>();
        if (raceService != null)
        {
            raceService.StartRace();
            Debug.Log("Race started");
        }

        // Для ПК (тренер) показываем дашборд
        if (!isAthleteMode && dashboardDocument != null)
            dashboardDocument.rootVisualElement.style.display = DisplayStyle.Flex;
    }

    private List<RatingRecord> GetTestRatingRecords()
    {
        return new List<RatingRecord>
        {
            new RatingRecord(1, "Александр Рудсков", "10:32"),
            new RatingRecord(2, "Николай Валов", "11:15"),
            new RatingRecord(3, "Станислав Антипов", "12:48"),
            new RatingRecord(4, "Игорь Горемыка", "13:22"),
            new RatingRecord(5, "Дмитрий Петров", "14:05")
        };
    }

    private List<RouteData> GetTestRoutes()
    {
        return new List<RouteData>
        {
            new RouteData
            {
                Id = 1,
                Name = "Берёзовая Роща",
                ImagePath = "",
                Description = "Трасса 'Берёзовая Роща' — 8 км по медленному течению",
                Length = "8 км",
                Difficulty = "средняя",
                WaterType = "медленное течение"
            },
            new RouteData
            {
                Id = 2,
                Name = "Горная река",
                ImagePath = "",
                Description = "Экстремальный маршрут с порогами III-IV категории",
                Length = "12 км",
                Difficulty = "высокая",
                WaterType = "бурное течение"
            }
        };
    }

    private void UpdateCarousel(int index)
    {
        if (routes == null || index >= routes.Count) return;
        var route = routes[index];
        var root = routeScreenDocument.rootVisualElement;

        var descText = root.Q<Label>("RouteDescText");
        if (descText != null) descText.text = route.Description;

        var detailsLabel = root.Q<Label>("RouteDetails");
        if (detailsLabel != null)
            detailsLabel.text = $"{route.Length} | {route.Difficulty} | {route.WaterType}";

        UpdatePaginationDots(index);
    }

    private void UpdatePaginationDots(int activeIndex)
    {
        if (routeScreenDocument == null) return;
        var root = routeScreenDocument.rootVisualElement;
        var pagination = root.Q<VisualElement>("pagination");
        if (pagination == null) return;

        int dotIndex = 0;
        foreach (var dot in pagination.Children())
        {
            if (dotIndex == activeIndex)
                dot.AddToClassList("active");
            else
                dot.RemoveFromClassList("active");
            dotIndex++;
        }
    }

    private void OnDestroy()
    {
        // Отписываемся от MetricsCalculator
        if (metricsCalculator != null)
            metricsCalculator.OnMetricsUpdated -= UpdateDashboard;

        // Безопасная отписка от RaceService
        try
        {
            if (ServiceLocator.Instance != null)
            {
                var raceService = ServiceLocator.Instance.GetService<IRaceService>();
                if (raceService != null)
                    raceService.OnRaceFinished -= OnRaceFinished;
            }
        }
        catch (System.Exception e)
        {
            Debug.Log($"Could not unsubscribe from RaceService: {e.Message}");
        }
    }
}