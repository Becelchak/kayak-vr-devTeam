using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// ViewModel для панели погоды. Содержит команды и свойства для отображения.
/// Взаимодействует с IWeatherService.
/// </summary>
public class WeatherViewModel
{
    private readonly IWeatherService weatherService;
    private bool isRaining;

    #region Команды
    /// <summary>Команда переключения дождя.</summary>
    public RelayCommand ToggleRainCommand { get; }
    /// <summary>Команда установки солнечной погоды.</summary>
    public RelayCommand SetSunnyCommand { get; }
    /// <summary>Команда установки дождливой погоды.</summary>
    public RelayCommand SetRainyCommand { get; }
    /// <summary>Команда изменения направления течения (передаётся индекс состояния от 0 до 4).</summary>
    public RelayCommand<int> SetFlowDirectionCommand { get; }
    /// <summary>Команда изменения скорости течения (м/с).</summary>
    public RelayCommand<float> SetFlowSpeedCommand { get; }
    #endregion

    #region Свойства для отображения (обновляются через события)
    /// <summary>Текст для отображения направления течения.</summary>
    public string FlowDirectionText => $"{weatherService.CurrentFlowAngle:F0}°";
    /// <summary>Текст для отображения скорости течения.</summary>
    public string FlowSpeedText => $"{weatherService.CurrentFlowSpeed:F1} м/с";
    /// <summary>Идёт ли дождь.</summary>
    public bool IsRaining => isRaining;

    /// <summary>Событие изменения любого свойства (для обновления UI).</summary>
    public event Action PropertyChanged;
    #endregion

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