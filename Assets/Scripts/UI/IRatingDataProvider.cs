using System.Collections.Generic;

public interface IRatingDataProvider
{
    List<RatingRecord> GetRatingRecords();
    void RefreshRatingData(string period);
}

public struct RatingRecord
{
    public int Rank;
    public string Name;
    public string Time;

    public RatingRecord(int rank, string name, string time)
    {
        Rank = rank;
        Name = name;
        Time = time;
    }
}