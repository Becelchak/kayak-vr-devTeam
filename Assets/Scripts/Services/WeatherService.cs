using UnityEngine;
using Crest;
using System;

public class WeatherService : MonoBehaviour, IWeatherService
{
    [Header("Crest Flow")]
    [SerializeField] private bool useFlowVelocityField = true;

    [Header("Rain")]
    [SerializeField] private ParticleSystem rainParticleSystem;

    [Header("Skybox Materials")]
    public Material sunnySkybox;
    public Material rainySkybox;

    [Header("Fog Settings")]
    public Color sunnyFogColor = new Color(0.5f, 0.7f, 0.9f);
    public Color rainyFogColor = new Color(0.2f, 0.2f, 0.25f);
    public float sunnyFogDensity = 0.005f;
    public float rainyFogDensity = 0.03f;

    [Header("Transition")]
    [Tooltip("Скорость смены погоды")]
    public float transitionSpeed = 0.5f;

    private Material runtimeSkybox;
    private WeatherMode targetWeather = WeatherMode.Sunny;

    private float interpolationProgress = 0f;

    private OceanRenderer oceanRender;
    private SplinePointDataFlow flowSettings;
    private SplinePointDataWaves wavesSettings;

    private float currentSpeed = 0f;
    private float currentAngle = 0f;
    private bool isRaining = false;
    private bool environmentNeedsUpdate = false;

    private void Awake()
    {
        if (OceanRenderer.Instance != null)
        {
            oceanRender = OceanRenderer.Instance;
        }
        else
        {
            Debug.LogWarning("Flow simulation not enabled in OceanRenderer!");
        }

        RenderSettings.fog = true;

        if (sunnySkybox != null)
        {
            runtimeSkybox = new Material(sunnySkybox);
            RenderSettings.skybox = runtimeSkybox;
        }
    }

    private void Update()
    {
        float targetValue = (targetWeather == WeatherMode.Rainy) ? 1f : 0f;

        if (!Mathf.Approximately(interpolationProgress, targetValue))
        {
            interpolationProgress = Mathf.MoveTowards(interpolationProgress, targetValue, transitionSpeed * Time.deltaTime);
            ApplyVisualWeatherTransition(interpolationProgress);
            environmentNeedsUpdate = true;
        }
        else if (environmentNeedsUpdate)
        {
            DynamicGI.UpdateEnvironment();
            environmentNeedsUpdate = false;
        }
    }

    private void ApplyVisualWeatherTransition(float progress)
    {
        if (runtimeSkybox != null && sunnySkybox != null && rainySkybox != null)
        {
            runtimeSkybox.Lerp(sunnySkybox, rainySkybox, progress);
        }

        RenderSettings.fogColor = Color.Lerp(sunnyFogColor, rainyFogColor, progress);
        RenderSettings.fogDensity = Mathf.Lerp(sunnyFogDensity, rainyFogDensity, progress);

        if (progress > 0.3f && !isRaining)
        {
            SetRain(true);
        }
        else if (progress <= 0.3f && isRaining)
        {
            SetRain(false);
        }
    }
    public void SetWeatherMode(WeatherMode mode)
    {
        targetWeather = mode;
    }

    public void SetFlowDirection(float angleDegrees)
    {
        currentAngle = angleDegrees;
        ApplyFlow();
    }

    public void SetFlowSpeed(float speed)
    {
        currentSpeed = speed;
        ApplyFlow();
    }

    private void ApplyFlow()
    {
        if (flowSettings == null) return;

        if (useFlowVelocityField)
        {
            Vector2 direction = Quaternion.Euler(0, currentAngle, 0) * Vector2.up;
            flowSettings.FlowVelocity = direction.magnitude * currentSpeed;
        }
        else
        {
            flowSettings.FlowVelocity = currentSpeed;
        }

        wavesSettings.Weight = currentSpeed / 2;
        oceanRender._globalWindSpeed = currentSpeed / 3;

        Debug.Log($"Flow changed: angle={currentAngle}, speed={currentSpeed}");
    }

    public void SetRain(bool active)
    {
        if (rainParticleSystem == null) return;

        if (active && !rainParticleSystem.isPlaying)
        {
            rainParticleSystem.Play();
            isRaining = true;
        }
        else if (!active && rainParticleSystem.isPlaying)
        {
            rainParticleSystem.Stop();
            isRaining = false;
        }
    }

    public float CurrentFlowSpeed => currentSpeed;
    public float CurrentFlowAngle => currentAngle;
    public bool IsRaining => isRaining;
    public WeatherMode CurrentWeather => interpolationProgress > 0.5f ? WeatherMode.Rainy : WeatherMode.Sunny;
}

[Serializable]
public enum WeatherMode
{
    Sunny = 0,
    Rainy = 1,
}