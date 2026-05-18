using UnityEngine;
using UnityEngine.UIElements;

public class WeatherUIView : MonoBehaviour
{
    private WeatherViewModel viewModel;
    private UIDocument uiDocument;

    // Сохраняем ссылки на элементы, чтобы отписаться
    private Toggle rainToggle;
    private Button sunnyBtn;
    private Button rainyBtn;
    private SliderInt dirSlider;
    private Slider speedSlider;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        viewModel = new WeatherViewModel();

        // Подписка на обновления свойств
        viewModel.PropertyChanged += UpdateUI;
    }

    private void OnEnable()
    {
        var root = uiDocument.rootVisualElement;

        // Инициализация элементов
        rainToggle = root.Q<Toggle>("RainToggle");
        sunnyBtn = root.Q<Button>("SunnyButton");
        rainyBtn = root.Q<Button>("RainyButton");
        dirSlider = root.Q<SliderInt>("FlowDirectionSlider");
        speedSlider = root.Q<Slider>("FlowSpeedSlider");

        // Подписка на события UI
        if (rainToggle != null)
        {
            rainToggle.RegisterValueChangedCallback(OnRainToggleChanged);
            rainToggle.SetValueWithoutNotify(viewModel.IsRaining);
        }

        if (sunnyBtn != null)
            sunnyBtn.clicked += OnSunnyClicked;

        if (rainyBtn != null)
            rainyBtn.clicked += OnRainyClicked;

        if (dirSlider != null)
        {
            dirSlider.lowValue = 0;
            dirSlider.highValue = 4;
            dirSlider.RegisterValueChangedCallback(OnDirectionChanged);
            dirSlider.value = 0;
        }
            

        if (speedSlider != null)
            speedSlider.RegisterValueChangedCallback(OnSpeedChanged);

        // Обновить текстовые метки
        UpdateUI();
    }

    private void OnDisable()
    {
        // Отписка от событий во избежание утечек памяти
        if (rainToggle != null)
            rainToggle.UnregisterValueChangedCallback(OnRainToggleChanged);

        if (sunnyBtn != null)
            sunnyBtn.clicked -= OnSunnyClicked;

        if (rainyBtn != null)
            rainyBtn.clicked -= OnRainyClicked;

        if (dirSlider != null)
            dirSlider.UnregisterValueChangedCallback(OnDirectionChanged);

        if (speedSlider != null)
            speedSlider.UnregisterValueChangedCallback(OnSpeedChanged);
    }

    private void OnDestroy()
    {
        // Отписка от события ViewModel
        if (viewModel != null)
            viewModel.PropertyChanged -= UpdateUI;
    }

    // Обработчики UI событий
    private void OnRainToggleChanged(ChangeEvent<bool> evt)
    {
        viewModel.ToggleRainCommand.Execute();
        // UI синхронизируется через UpdateUI, но для мгновенности можно и так:
        // rainToggle.SetValueWithoutNotify(viewModel.IsRaining);
    }

    private void OnSunnyClicked() => viewModel.SetSunnyCommand.Execute();
    private void OnRainyClicked() => viewModel.SetRainyCommand.Execute();
    private void OnDirectionChanged(ChangeEvent<int> evt) => viewModel.SetFlowDirectionCommand.Execute(evt.newValue);
    private void OnSpeedChanged(ChangeEvent<float> evt) => viewModel.SetFlowSpeedCommand.Execute(evt.newValue);

    private void UpdateUI()
    {
        var root = uiDocument?.rootVisualElement;
        if (root == null) return;

        var dirLabel = root.Q<Label>("FlowDirectionValue");
        if (dirLabel != null)
            dirLabel.text = viewModel.FlowDirectionText;

        var speedLabel = root.Q<Label>("FlowSpeedValue");
        if (speedLabel != null)
            speedLabel.text = viewModel.FlowSpeedText;

        if (rainToggle != null)
            rainToggle.SetValueWithoutNotify(viewModel.IsRaining);
    }
}