using System.Collections.Generic;

public interface ICarouselDataProvider
{
    List<RouteData> GetRoutes();
    void SelectRoute(int routeId);
}

public struct RouteData
{
    public int Id;
    public string Name;
    public string ImagePath;
    public string Description;
    public string Length;
    public string Difficulty;
    public string WaterType;
}