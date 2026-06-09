using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class GraphController
{
    private VisualElement chartArea;

    public System.Action OnPrevPeriod;
    public System.Action OnNextPeriod;

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

    public void LoadTestData()
    {
        var testData = new List<(float time, float value)>
        {
            (0, 3.2f), (1, 3.5f), (2, 3.8f), (3, 4.0f), (4, 3.9f),
            (5, 4.2f), (6, 4.5f), (7, 4.3f), (8, 4.1f), (9, 4.4f)
        };
        UpdateGraph(testData);
        Debug.Log("Test graph data loaded");
    }

    public void UpdateGraph(List<(float time, float value)> data)
    {
        if (chartArea == null) return;

        chartArea.Clear();

        var label = new Label($"Graph: {data.Count} points");
        label.style.color = Color.white;
        chartArea.Add(label);

        Debug.Log($"Graph updated with {data.Count} points");
    }
}