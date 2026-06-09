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

    private string selectedRole = "athlete";

    // Элементы дашборда
    private Label tempoValue;
    private Label strokeValue;
    private Label speedValue;
    private Label tempoPrevValue;
    private Label strokePrevValue;
    private Label speedPrevValue;
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

    private void Start()
    {
        SetupDashboard();
        SetupRatingTable();
        SetupCarousel();
        SetupButtons();
        SetupCursor();
        SetupGraph();

        if (metricsCalculator != null)
            metricsCalculator.OnMetricsUpdated += UpdateDashboard;
        else
            Debug.LogError("MetricsCalculator is NULL! Assign it in Inspector.");
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
        tempoPrevValue = root.Q<Label>("TempoPrevValue");
        strokePrevValue = root.Q<Label>("StrokePrevValue");
        speedPrevValue = root.Q<Label>("SpeedPrevValue");
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
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
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
        graphController.LoadTestData();

        graphController.OnPrevPeriod += () => Debug.Log("Previous period clicked");
        graphController.OnNextPeriod += () => Debug.Log("Next period clicked");

        Debug.Log("Graph setup complete");
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
        if (tempoPrevValue != null)
            tempoPrevValue.text = $"{previousStrokeRate:F1}с";

        if (tempoArrow != null)
        {
            bool isBetter = currentValue < standardTempo;
            tempoArrow.text = isBetter ? "▲" : "▼";

            if (isBetter)
            {
                tempoArrow.RemoveFromClassList("red-arrow");
                tempoArrow.AddToClassList("green-arrow");
            }
            else
            {
                tempoArrow.RemoveFromClassList("green-arrow");
                tempoArrow.AddToClassList("red-arrow");
            }
        }
    }

    private void UpdateStrokeMetric(float currentValue)
    {
        if (strokePrevValue != null)
            strokePrevValue.text = $"{previousStrokeLength:F0}";

        if (strokeArrow != null)
        {
            bool isBetter = currentValue > standardStrokeLength;
            strokeArrow.text = isBetter ? "▲" : "▼";

            if (isBetter)
            {
                strokeArrow.RemoveFromClassList("red-arrow");
                strokeArrow.AddToClassList("green-arrow");
            }
            else
            {
                strokeArrow.RemoveFromClassList("green-arrow");
                strokeArrow.AddToClassList("red-arrow");
            }
        }
    }

    private void UpdateSpeedMetric(float currentValue)
    {
        if (speedPrevValue != null)
            speedPrevValue.text = $"{previousSpeed:F1}м/с";

        if (speedArrow != null)
        {
            bool isBetter = currentValue > standardSpeed;
            speedArrow.text = isBetter ? "▲" : "▼";

            if (isBetter)
            {
                speedArrow.RemoveFromClassList("red-arrow");
                speedArrow.AddToClassList("green-arrow");
            }
            else
            {
                speedArrow.RemoveFromClassList("green-arrow");
                speedArrow.AddToClassList("red-arrow");
            }
        }
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
        return currentGraphType == 0 ? "Скорость (м/с)" :
               currentGraphType == 1 ? "Темп (греб/мин)" : "Длина гребка (м)";
    }

    private void UpdateGraphByType()
    {
        if (graphController == null) return;

        List<(float time, float value)> testData;

        if (currentGraphType == 0) // Скорость
        {
            testData = new List<(float, float)> { (0, 3.2f), (1, 3.8f), (2, 4.2f), (3, 4.0f), (4, 4.5f) };
        }
        else if (currentGraphType == 1) // Темп
        {
            testData = new List<(float, float)> { (0, 55f), (1, 58f), (2, 60f), (3, 57f), (4, 59f) };
        }
        else // Длина гребка
        {
            testData = new List<(float, float)> { (0, 110f), (1, 115f), (2, 120f), (3, 118f), (4, 122f) };
        }

        graphController.UpdateGraph(testData);
    }

    private void LoadRatingData()
    {
        if (ratingDataProvider != null)
        {
            var records = ratingDataProvider.GetRatingRecords();
            UpdateRatingTable(records);
        }
        else
        {
            UpdateRatingTable(GetTestRatingRecords());
        }
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

        // Здесь должна быть логика включения камер
        // В зависимости от роли (спортсмен/тренер)

        // Пример: включаем XR Origin для спортсмена
        if (athleteSystem != null)
            athleteSystem.SetActive(true);

        // Если нужно показать дашборд для тренера
        if (dashboardDocument != null && selectedRole != "athlete")
            dashboardDocument.rootVisualElement.style.display = DisplayStyle.Flex;

        // Отключаем курсор для VR
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
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
        if (metricsCalculator != null)
            metricsCalculator.OnMetricsUpdated -= UpdateDashboard;
    }
}