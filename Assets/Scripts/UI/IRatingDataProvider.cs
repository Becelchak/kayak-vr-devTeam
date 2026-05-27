using System.Collections.Generic;

public interface IRatingDataProvider
{
    // Получить список рекордов (место, имя, время)
    List<RatingRecord> GetRatingRecords();

    // Обновить данные (например, за другой месяц)
    void RefreshRatingData(string period);
}

// Структура одной записи в таблице
public struct RatingRecord
{
    public int Rank;        // Место (1, 2, 3...)
    public string Name;     // Имя игрока
    public string Time;     // Время (в формате "10:32" или "10 мин")

    public RatingRecord(int rank, string name, string time)
    {
        Rank = rank;
        Name = name;
        Time = time;
    }
}