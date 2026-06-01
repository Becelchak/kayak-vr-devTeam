using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// View-компонент панели управления погодой. Привязывает UI-элементы к WeatherViewModel.
/// Управляет блокировкой камеры при активации окна (если используется).
/// </summary>
public class WeatherUIView : MonoBehaviour
{
    private WeatherViewModel viewModel;
    private UIDocument uiDocument;

    private Toggle rainToggle;
    private Button sunnyBtn;
    private Button rainyBtn;
    private SliderInt dirSlider;
    private Slider speedSlider;

    private void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        viewModel = new WeatherViewModel();

        viewModel.PropertyChanged += UpdateUI;
    }

    private void OnEnable()
    {
        var root = uiDocument.rootVisualElement;

        sunnyBtn = root.Q<Button>("SunnyButton");
        rainyBtn = root.Q<Button>("RainyButton");
        dirSlider = root.Q<SliderInt>("FlowDirectionSlider");
        speedSlider = root.Q<Slider>("FlowSpeedSlider");

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

        UpdateUI();
    }

    private void OnDisable()
    {

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
        if (viewModel != null)
            viewModel.PropertyChanged -= UpdateUI;
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