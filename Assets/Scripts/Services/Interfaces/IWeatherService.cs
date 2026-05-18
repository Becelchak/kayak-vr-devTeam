public interface IWeatherService
{
    void SetRain(bool active);
    void SetFlowDirection(int state);
    void SetFlowSpeed(float speed);
    float CurrentFlowSpeed { get; }
    float CurrentFlowAngle { get; }
    bool IsRaining { get; }
}