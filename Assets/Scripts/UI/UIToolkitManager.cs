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
    private float standardTempo = 4.1f;      // стандартный темп (сек)
    private float standardStrokeLength = 130; // стандартная длина гребка (см)
    private float standardSpeed = 3.8f;       // стандартная скорость (м/с)

    // Элементы таблицы
    private VisualElement statsList;
    private Button fullTableButton;

    // Элементы карусели
    private int currentRouteIndex;
    private List<RouteData> routes;

    private void Start()
    {
        SetupDashboard();
        SetupRatingTable();
        SetupCarousel();
        SetupButtons();
        SetupCursor();
        SetupGraph();

        Invoke(nameof(ForceShowCursor), 0.1f);

        if (metricsCalculator != null)
            metricsCalculator.OnMetricsUpdated += UpdateDashboard;
    }

    private void ForceShowCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Debug.Log("Cursor forced visible");
    }

    private void SetupDashboard()
    {
        if (dashboardDocument == null)
        {
            Debug.LogWarning("Dashboard Document not assigned!");
            return;
        }

        var root = dashboardDocument.rootVisualElement;
        if (root == null)
        {
            Debug.LogError("root is NULL! Dashboard UXML may not be loaded.");
            return;
        }
        // Текущие значения
        tempoValue = root.Q<Label>("TempoValue");
        strokeValue = root.Q<Label>("StrokeValue");
        speedValue = root.Q<Label>("SpeedValue");

        // Предыдущие значения
        tempoPrevValue = root.Q<Label>("TempoPrevValue");
        strokePrevValue = root.Q<Label>("StrokePrevValue");
        speedPrevValue = root.Q<Label>("SpeedPrevValue");

        // Стрелки
        tempoArrow = root.Q<Label>("TempoArrow");
        strokeArrow = root.Q<Label>("StrokeArrow");
        speedArrow = root.Q<Label>("SpeedArrow");

        // Инициализация предыдущих значений
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
    }

    private void SetupCursor()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void SetupGraph()
    {
        if (routeScreenDocument == null) return;
        var root = routeScreenDocument.rootVisualElement;
        var chartArea = root.Q<VisualElement>("ChartArea");

        if (chartArea != null)
        {
            var graphController = new GraphController(chartArea);
            graphController.OnPrevPeriod += () => Debug.Log("Previous period");
            graphController.OnNextPeriod += () => Debug.Log("Next period");
        }
    }

    // ========== Обновление дашборда ==========

    private void UpdateDashboard(float strokeRate, float strokeLength, float speed)
    {
        // Обновляем текущие значения
        if (tempoValue != null) tempoValue.text = $"{strokeRate:F1}с";
        if (strokeValue != null) strokeValue.text = $"{strokeLength:F0}";
        if (speedValue != null) speedValue.text = $"{speed:F1}м/с";

        // Обновляем каждую метрику с её правилами
        UpdateTempoMetric(strokeRate);
        UpdateStrokeMetric(strokeLength);
        UpdateSpeedMetric(speed);

        // Сохраняем текущие значения как предыдущие для следующего обновления
        previousStrokeRate = strokeRate;
        previousStrokeLength = strokeLength;
        previousSpeed = speed;
    }

    /// <summary>
    /// Темп (500м): зеленый если МЕНЬШЕ стандарта (быстрее), красный если БОЛЬШЕ (медленнее)
    /// </summary>
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

    /// <summary>
    /// Длина гребка: зеленый если БОЛЬШЕ стандарта (длиннее), красный если МЕНЬШЕ (короче)
    /// </summary>
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

    /// <summary>
    /// Скорость: зеленый если БОЛЬШЕ стандарта (быстрее), красный если МЕНЬШЕ (медленнее)
    /// </summary>
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

    // ========== Таблица рекордов ==========

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
        {
            statsList.Add(CreateRatingRow(record));
        }
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

    private void OnFullTableClick()
    {
        Debug.Log("Full table button clicked");
    }

    // ========== Карусель ==========

    private void OnLeftArrowClick()
    {
        if (routes == null || routes.Count == 0) return;
        currentRouteIndex--;
        if (currentRouteIndex < 0) currentRouteIndex = routes.Count - 1;
        UpdateCarousel(currentRouteIndex);
        carouselDataProvider?.SelectRoute(routes[currentRouteIndex].Id);
    }

    private void OnRightArrowClick()
    {
        if (routes == null || routes.Count == 0) return;
        currentRouteIndex++;
        if (currentRouteIndex >= routes.Count) currentRouteIndex = 0;
        UpdateCarousel(currentRouteIndex);
        carouselDataProvider?.SelectRoute(routes[currentRouteIndex].Id);
    }

    private void UpdateCarousel(int index)
    {
        if (routes == null || index >= routes.Count) return;

        var route = routes[index];
        var root = routeScreenDocument.rootVisualElement;

        // Обновляем изображение
        var routeImage = root.Q<VisualElement>("RouteImage");
        if (routeImage != null && !string.IsNullOrEmpty(route.ImagePath))
        {
            Debug.Log($"Update route image: {route.ImagePath}");
        }

        // Обновляем описание
        var descText = root.Q<Label>("RouteDescText");
        if (descText != null)
            descText.text = route.Description;

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

    // ========== Кнопка старта ==========

    private void OnStartTraining()
    {
        Debug.Log("Start training button clicked");

        // Скрываем UI экраны
        if (routeScreenDocument != null)
            routeScreenDocument.rootVisualElement.style.display = DisplayStyle.None;

        if (dashboardDocument != null)
            dashboardDocument.rootVisualElement.style.display = DisplayStyle.None;

        // Здесь будет логика включения камер в зависимости от роли
        // if (selectedRole == "athlete") athleteSystem.SetActive(true);
        // else coachSystem.SetActive(true);
    }

    // ========== Тестовые данные ==========

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
                ImagePath = "Assets/UI/Images/route1.png",
                Description = "Трасса 'Берёзовая Роща' — 8 км по медленному течению с порогами I–II; проходит через берёзовые леса и живописные заливки. Время прохождения 3–4 часа.",
                Length = "8 км",
                Difficulty = "средняя",
                WaterType = "медленное течение"
            },
            new RouteData
            {
                Id = 2,
                Name = "Горная река",
                ImagePath = "Assets/UI/Images/route2.png",
                Description = "Экстремальный маршрут с порогами III-IV категории. Требует хорошей подготовки и снаряжения.",
                Length = "12 км",
                Difficulty = "высокая",
                WaterType = "бурное течение"
            }
        };
    }

    private void OnDestroy()
    {
        if (metricsCalculator != null)
            metricsCalculator.OnMetricsUpdated -= UpdateDashboard;
    }
}