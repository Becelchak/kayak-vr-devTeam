using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

public class WeatherViewModel
{
    private readonly IWeatherService weatherService;
    private bool isRaining;

    // Команды
    public RelayCommand ToggleRainCommand { get; }
    public RelayCommand SetSunnyCommand { get; }
    public RelayCommand SetRainyCommand { get; }
    public RelayCommand<int> SetFlowDirectionCommand { get; }
    public RelayCommand<float> SetFlowSpeedCommand { get; }

    // Свойства для отображения (обновляются через события)
    public string FlowDirectionText => $"{weatherService.CurrentFlowAngle:F0}°";
    public string FlowSpeedText => $"{weatherService.CurrentFlowSpeed:F1} м/с";
    public bool IsRaining => isRaining;

    public event Action PropertyChanged;

    public WeatherViewModel()
    {
        weatherService = ServiceLocator.Instance.GetService<WeatherService>();

        isRaining = weatherService.IsRaining;

        ToggleRainCommand = new RelayCommand(ToggleRain);
        SetSunnyCommand = new RelayCommand(() => SetWeatherMode(WeatherMode.Sunny));
        SetRainyCommand = new RelayCommand(() => SetWeatherMode(WeatherMode.Rainy));
        SetFlowDirectionCommand = new RelayCommand<int>(state => SetFlowDirectionFromService(state));
        SetFlowSpeedCommand = new RelayCommand<float>(spd => SetFlowDirectionFromService(spd));
    }

    private void ToggleRain()
    {
        weatherService.SetRain(!weatherService.IsRaining);
        OnPropertyChanged(nameof(IsRaining));
    }

    private void SetWeatherMode(WeatherMode mode)
    {
        if (weatherService is WeatherService ws)
            ws.SetWeatherMode(mode);
        OnPropertyChanged(nameof(IsRaining));
    }

    private void SetFlowDirectionFromService(int state)
    {
        weatherService.SetFlowDirection(state);
        OnPropertyChanged(nameof(SetFlowDirectionCommand));
    }

    private void SetFlowDirectionFromService(float speed)
    {
        weatherService.SetFlowSpeed(speed);
        OnPropertyChanged(nameof(SetFlowDirectionCommand));
    }

    private void OnPropertyChanged(string propName) => PropertyChanged?.Invoke();
}