using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class GraphController
{
    private VisualElement chartArea;
    private List<(float time, float value)> currentData;
    private string currentDataType = "speed";

    public System.Action OnPrevPeriod;
    public System.Action OnNextPeriod;

    // Цвета графика
    private Color barColor = Color.black;                        // голубые колонки
    private Color lineColor = Color.yellow;                      // жёлтая линия

    public GraphController(VisualElement chartArea)
    {
        this.chartArea = chartArea;

        var leftArrow = chartArea.parent?.Q<Button>("ChartLeftArrow");
        var rightArrow = chartArea.parent?.Q<Button>("ChartRightArrow");

        leftArrow?.RegisterCallback<ClickEvent>(_ => OnPrevPeriod?.Invoke());
        rightArrow?.RegisterCallback<ClickEvent>(_ => OnNextPeriod?.Invoke());

        chartArea.AddToClassList("chart-ready");
        Debug.Log("GraphController initialized");
    }

    // ===== ТЕСТОВЫЕ ДАННЫЕ =====
    public void LoadTestData(string dataType = "speed")
    {
        currentDataType = dataType;

        if (dataType == "speed")
        {
            currentData = new List<(float, float)>
            {
                (0, 2.5f), (1, 2.8f), (2, 3.2f), (3, 3.5f), (4, 3.8f),
                (5, 4.0f), (6, 4.2f), (7, 4.1f), (8, 3.9f), (9, 4.0f),
                (10, 4.3f), (11, 4.5f), (12, 4.4f), (13, 4.2f), (14, 4.0f)
            };
        }
        else if (dataType == "tempo")
        {
            currentData = new List<(float, float)>
            {
                (0, 45f), (1, 48f), (2, 52f), (3, 55f), (4, 58f),
                (5, 60f), (6, 59f), (7, 58f), (8, 56f), (9, 57f),
                (10, 58f), (11, 60f), (12, 61f), (13, 59f), (14, 58f)
            };
        }
        else if (dataType == "stroke")
        {
            currentData = new List<(float, float)>
            {
                (0, 95f), (1, 100f), (2, 105f), (3, 110f), (4, 115f),
                (5, 120f), (6, 122f), (7, 125f), (8, 123f), (9, 120f),
                (10, 118f), (11, 115f), (12, 112f), (13, 110f), (14, 108f)
            };
        }

        UpdateGraph(currentData);
        Debug.Log($"Test graph data loaded: {dataType}");
    }

    public void UpdateGraph(List<(float time, float value)> data)
    {
        if (chartArea == null) return;

        currentData = data;
        chartArea.Clear();

        if (data == null || data.Count == 0)
        {
            var emptyLabel = new Label("Нет данных для отображения");
            emptyLabel.style.color = Color.white;
            emptyLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            chartArea.Add(emptyLabel);
            return;
        }

        var container = new VisualElement();
        container.style.flexDirection = FlexDirection.Column;
        container.style.width = new Length(100, LengthUnit.Percent);
        container.style.height = new Length(100, LengthUnit.Percent);

        // Заголовок
        var titleLabel = new Label(GetDataTypeName());
        titleLabel.style.color = Color.black;
        titleLabel.style.fontSize = 14;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.marginBottom = 5;
        titleLabel.style.unityTextAlign = TextAnchor.MiddleCenter;  // по центру
        container.Add(titleLabel);

        // Контейнер для графика (относительное позиционирование для линии)
        var graphContainer = new VisualElement();
        graphContainer.style.flexDirection = FlexDirection.Row;
        graphContainer.style.alignItems = Align.FlexEnd;
        graphContainer.style.height = new Length(80, LengthUnit.Percent);
        graphContainer.style.position = Position.Relative;

        float maxValue = GetMaxValue(data);
        float minValue = GetMinValue(data);
        float range = maxValue - minValue;
        if (range < 0.1f) range = 1f;

        // Сохраняем позиции колонок для линии
        List<Rect> columnPositions = new List<Rect>();

        // Рисуем колонки
        for (int i = 0; i < data.Count; i++)
        {
            var point = data[i];
            float heightPercent = ((point.value - minValue) / range) * 100;

            var column = new VisualElement();
            column.style.width = 20;
            column.style.height = new Length(heightPercent, LengthUnit.Percent);
            column.style.backgroundColor = barColor;
            column.style.marginLeft = 4;
            column.style.marginRight = 4;
            column.style.alignSelf = Align.FlexEnd;
            column.style.borderTopLeftRadius = 4;
            column.style.borderTopRightRadius = 4;

            // Тултип при наведении
            var tooltip = new Label($"{point.time:F0}с: {point.value:F1}");
            tooltip.style.position = Position.Absolute;
            tooltip.style.bottom = new Length(heightPercent + 5, LengthUnit.Percent);
            tooltip.style.left = 0;
            tooltip.style.backgroundColor = new Color(0, 0, 0, 0.7f);
            tooltip.style.color = Color.white;
            tooltip.style.fontSize = 9;
            tooltip.style.whiteSpace = WhiteSpace.Normal;
            tooltip.style.display = DisplayStyle.None;
            column.Add(tooltip);

            column.RegisterCallback<MouseEnterEvent>(_ => tooltip.style.display = DisplayStyle.Flex);
            column.RegisterCallback<MouseLeaveEvent>(_ => tooltip.style.display = DisplayStyle.None);

            graphContainer.Add(column);

            // Сохраняем позицию для линии
            column.RegisterCallback<GeometryChangedEvent>(e =>
            {
                var rect = column.layout;
                columnPositions.Add(rect);
                DrawLine(graphContainer, columnPositions);
            });
        }

        container.Add(graphContainer);

        // Ось Y (значения)
        var axisContainer = new VisualElement();
        axisContainer.style.flexDirection = FlexDirection.Row;
        axisContainer.style.justifyContent = Justify.SpaceBetween;
        axisContainer.style.marginTop = 10;

        axisContainer.Add(new Label($"{minValue:F1}") { style = { color = Color.gray, fontSize = 10 } });
        axisContainer.Add(new Label($"{(minValue + maxValue) / 2:F1}") { style = { color = Color.gray, fontSize = 10 } });
        axisContainer.Add(new Label($"{maxValue:F1}") { style = { color = Color.gray, fontSize = 10 } });
        container.Add(axisContainer);

        // Ось X (время)
        var timeContainer = new VisualElement();
        timeContainer.style.flexDirection = FlexDirection.Row;
        timeContainer.style.justifyContent = Justify.SpaceBetween;
        timeContainer.style.marginTop = 5;

        if (data.Count > 0)
        {
            timeContainer.Add(new Label($"{data[0].time:F0}с") { style = { color = Color.gray, fontSize = 10 } });
            timeContainer.Add(new Label($"{data[data.Count - 1].time:F0}с") { style = { color = Color.gray, fontSize = 10 } });
        }
        container.Add(timeContainer);

        chartArea.Add(container);
    }

    private void DrawLine(VisualElement graphContainer, List<Rect> positions)
    {
        if (positions.Count < 2) return;

        // Удаляем старые линии
        foreach (var element in graphContainer.Children())
        {
            if (element.ClassListContains("graph-line"))
            {
                element.RemoveFromHierarchy();
            }
        }

        // Рисуем линии между колонками
        for (int i = 0; i < positions.Count - 1; i++)
        {
            float startX = positions[i].x + positions[i].width / 2;
            float startY = positions[i].y;
            float endX = positions[i + 1].x + positions[i + 1].width / 2;
            float endY = positions[i + 1].y;

            var line = new VisualElement();
            line.AddToClassList("graph-line");
            line.style.position = Position.Absolute;
            line.style.backgroundColor = lineColor;
            line.style.height = 2;

            float deltaX = endX - startX;
            float deltaY = endY - startY;
            float distance = Mathf.Sqrt(deltaX * deltaX + deltaY * deltaY);
            float angle = Mathf.Atan2(deltaY, deltaX) * Mathf.Rad2Deg;

            line.style.width = distance;
            line.style.left = startX;
            line.style.top = startY;
            line.style.rotate = new Rotate(Angle.Degrees(angle));

            graphContainer.Add(line);
        }
    }

    private string GetDataTypeName()
    {
        switch (currentDataType)
        {
            case "speed": return "СКОРОСТЬ (м/с)";
            case "tempo": return "ТЕМП (греб/мин)";
            case "stroke": return "ДЛИНА ГРЕБКА (см)";
            default: return "ГРАФИК";
        }
    }

    private float GetMaxValue(List<(float time, float value)> data)
    {
        float max = data[0].value;
        foreach (var d in data) if (d.value > max) max = d.value;
        return max;
    }

    private float GetMinValue(List<(float time, float value)> data)
    {
        float min = data[0].value;
        foreach (var d in data) if (d.value < min) min = d.value;
        return min;
    }
}