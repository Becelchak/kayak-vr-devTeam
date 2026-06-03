using UnityEngine;
using Crest;
using System;
using System.Collections.Generic;
using Crest.Spline;

/// <summary>
/// Сервис управления погодой: дождь/солнечно, течение и направление волн.
/// Реализует плавный переход между солнечной и дождливой погодой.
/// </summary>
public class WeatherService : BaseService, IWeatherService
{
    [Header("Crest Flow")]
    [Tooltip("Использовать поле скорости потока (иначе — скорость сплайна)")]
    [SerializeField] private bool useFlowVelocityField = true;

    [Header("Rain")]
    [Tooltip("Система частиц дождя")]
    [SerializeField] private ParticleSystem rainParticleSystem;

    [Header("Skybox Materials (Skybox/Cubemap)")]
    [Tooltip("Материал скайбокса для солнечной погоды")]
    public Material sunnySkybox;
    [Tooltip("Материал скайбокса для дождливой погоды")]
    public Material rainySkybox;

    [Header("Fog Settings")]
    [Tooltip("Цвет тумана при солнце")]
    public Color sunnyFogColor = new Color(0.5f, 0.7f, 0.9f);
    [Tooltip("Цвет тумана при дожде")]
    public Color rainyFogColor = new Color(0.2f, 0.2f, 0.25f);
    [Tooltip("Плотность тумана при солнце")]
    public float sunnyFogDensity = 0.005f;
    [Tooltip("Плотность тумана при дожде")]
    public float rainyFogDensity = 0.03f;

    [Header("Transition")]
    [Tooltip("Скорость смены погоды")]
    public float transitionSpeed = 0.5f;

    [Header("Other")]
    [Tooltip("Родительский объект сплайна течения (IsetSplineRiver)")]
    [SerializeField] private GameObject splitPointParent;
    private ShapeFFT shapeFFTComponent;

    [Tooltip("Материал скайбокса")]
    private Material runtimeSkybox;
    private WeatherMode targetWeather = WeatherMode.Sunny;

    [Tooltip("Прогрессия перехода от солнечной погоды к дождливой и наоборот. 0.0f (Солнце) -> 1.0f (Дождь)")]
    private float interpolationProgress = 0f;

    private OceanRenderer oceanRender;
    private List<SplinePointDataFlow> flowSettings = new();
    private List<SplinePointDataWaves> wavesSettings = new();

    private float currentSpeed = 0f;
    private float currentAngle = 0f;
    private float speedMultiplier = 1f;
    private bool isRaining = false;
    private bool environmentNeedsUpdate = false;

    // Кешируем ID свойств шейдера для оптимизации производительности
    private static readonly int ExposureID = Shader.PropertyToID("_Exposure");
    private static readonly int TintID = Shader.PropertyToID("_Tint");
    private static readonly int TexID = Shader.PropertyToID("_Tex");

    private readonly (float angle, float speedMultiplier)[] states = new (float, float)[]
    {
        (0f, 0f),      // 0° - Стоячая вода
        (90f, 1f),     // 90° - По оси Z
        (180f, 1f),    // 180° - По оси X
        (-90f, -1f),   // -90° - Против оси Z
        (-180f, 1f)   // -180° - Против оси X
    };

    private void Awake()
    {
        base.Awake();

        RenderSettings.fog = true;

        if (sunnySkybox != null)
        {
            runtimeSkybox = new Material(sunnySkybox);
            RenderSettings.skybox = runtimeSkybox;
        }
    }

    private void Start()
    {
        if (OceanRenderer.Instance != null)
        {
            oceanRender = OceanRenderer.Instance;
            flowSettings.AddRange(splitPointParent.GetComponentsInChildren<SplinePointDataFlow>());
            wavesSettings.AddRange(splitPointParent.GetComponentsInChildren<SplinePointDataWaves>());
            shapeFFTComponent = splitPointParent.GetComponent<ShapeFFT>();
        }
        else
        {
            Debug.LogWarning("Flow simulation not enabled in OceanRenderer!");
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
            Cubemap sunTex = sunnySkybox.GetTexture(TexID) as Cubemap;
            Cubemap rainTex = rainySkybox.GetTexture(TexID) as Cubemap;

            float sunMaxExposure = sunnySkybox.GetFloat(ExposureID);
            float rainMaxExposure = rainySkybox.GetFloat(ExposureID);

            Color sunColor = sunnySkybox.GetColor(TintID);
            Color rainColor = rainySkybox.GetColor(TintID);

            // Реализуем плавный переход через изменение параметров
            if (progress < 0.5f)
            {
                runtimeSkybox.SetTexture(TexID, sunTex);

                float localProgress = Mathf.InverseLerp(0f, 0.5f, progress);
                runtimeSkybox.SetFloat(ExposureID, Mathf.Lerp(sunMaxExposure, 0f, localProgress));
                runtimeSkybox.SetColor(TintID, Color.Lerp(sunColor, Color.gray, localProgress));
            }
            else
            {
                runtimeSkybox.SetTexture(TexID, rainTex);

                float localProgress = Mathf.InverseLerp(0.5f, 1f, progress);
                runtimeSkybox.SetFloat(ExposureID, Mathf.Lerp(0f, rainMaxExposure, localProgress));
                runtimeSkybox.SetColor(TintID, Color.Lerp(Color.gray, rainColor, localProgress));
            }
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

    public void SetFlowDirection(int state)
    {
        currentAngle = states[state].angle;
        speedMultiplier = states[state].speedMultiplier;
        ApplyFlowDirection();
        ApplyFlow();
    }

    private void ApplyFlowDirection()
    {
        if (splitPointParent == null) return;

        float splineRotationAngle;
        float waveAngle;

        // Если угол 0 – течение отключено (скорость 0), можно не вращать
        if (Mathf.Approximately(currentAngle, 0f))
        {
            splineRotationAngle = 0f;
            waveAngle = 0f;
        }
        else
        {
            // Волны всегда направлены по оси Z: 90° – вперёд, -90° – назад
            waveAngle = currentAngle > 0 ? 90f : -90f;

            // Вычисляем поворот сплайна в зависимости от угла
            if (Mathf.Approximately(Mathf.Abs(currentAngle), 90f))
            {
                // Для 90° и -90° сплайн не поворачиваем – течение идёт вдоль Z
                splineRotationAngle = 0f;
            }
            else if (Mathf.Approximately(Mathf.Abs(currentAngle), 180f))
            {
                // Для 180° поворачиваем сплайн на +90° (течение вдоль X+)
                // Для -180° поворачиваем на -90° (течение вдоль X-)
                splineRotationAngle = currentAngle > 0 ? 90f : -90f;
            }
            else
            {
                // Если угол другой (не 0, ±90, ±180) – используем его напрямую (на всякий случай)
                splineRotationAngle = currentAngle;
            }
        }

        splitPointParent.transform.rotation = Quaternion.Euler(0, splineRotationAngle, 0);

        if (shapeFFTComponent != null)
        {
            shapeFFTComponent._waveDirectionHeadingAngle = waveAngle;
        }
    }

    public void SetFlowSpeed(float speed)
    {
        //Debug.Log($"flow speed = {speed}");
        currentSpeed = speed;
        ApplyFlow();
    }

    private void ApplyFlow()
    {
        if (flowSettings == null) return;

        if (useFlowVelocityField)
        {
            Vector2 direction = Quaternion.Euler(0, currentAngle, 0) * Vector2.up;
            foreach (var f in flowSettings)
            {
                f.FlowVelocity = direction.magnitude * currentSpeed * speedMultiplier;
            }
        }
        else
        {
            foreach (var f in flowSettings)
            {
                f.FlowVelocity = currentSpeed * speedMultiplier;
                ForceUpdateSpline(f);
            }
        }

        foreach(var w in wavesSettings)
        {
            w.Weight = currentSpeed / 2;
            ForceUpdateSpline(w);
        }
        if (oceanRender == null)
            return;
        oceanRender._globalWindSpeed = currentSpeed;

        //Debug.Log($"Flow changed: angle={currentAngle}, speed={currentSpeed}");
    }

    private void ForceUpdateSpline(SplinePointDataBase pointData)
    {
        if (pointData == null) return;
        var spline = pointData.GetComponentInParent<Spline>();
        if (spline != null)
        {
            spline.UpdateSpline();
        }
        else
        {
            var flowInput = pointData.GetComponentInParent<RegisterFlowInput>();
            flowInput?.OnSplineChange();
        }
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

    protected override Type GetServiceType() => typeof(IWeatherService);

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