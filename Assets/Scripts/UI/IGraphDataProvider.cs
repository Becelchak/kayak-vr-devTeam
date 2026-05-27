using System.Collections.Generic;

public interface IGraphDataProvider
{
    List<(float time, float value)> GetGraphData();
    void RefreshData(string period);
}