using UnityEngine;
using UnityEngine.UIElements;

public class GraphController
{
    private VisualElement chartArea;

    public System.Action OnPrevPeriod;
    public System.Action OnNextPeriod;

    public GraphController(VisualElement chartArea)
    {
        this.chartArea = chartArea;

        // Ищем стрелки внутри контейнера
        var leftArrow = chartArea.parent?.Q<Button>("ChartLeftArrow");
        var rightArrow = chartArea.parent?.Q<Button>("ChartRightArrow");

        leftArrow?.RegisterCallback<ClickEvent>(_ => OnPrevPeriod?.Invoke());
        rightArrow?.RegisterCallback<ClickEvent>(_ => OnNextPeriod?.Invoke());

        // Помечаем контейнер как готовый
        chartArea.AddToClassList("chart-ready");
        Debug.Log("Graph controller initialized. Ready for data.");
    }

    public void UpdateData(object data)
    {
        // Здесь будет обновление графика
        Debug.Log("Graph data updated");
    }
}