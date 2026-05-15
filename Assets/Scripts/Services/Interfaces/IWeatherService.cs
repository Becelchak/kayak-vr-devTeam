public interface IWeatherService
{
    void SetRain(bool active);
    void SetFlowDirection(float angleDegrees);
    void SetFlowSpeed(float speed);
    float CurrentFlowSpeed { get; }
    float CurrentFlowAngle { get; }
    bool IsRaining { get; }
}