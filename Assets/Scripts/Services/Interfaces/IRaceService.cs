using System;
using UnityEngine;

public interface IRaceService
{
    event Action<RaceStatistics> OnRaceFinished;
    void StartRace();
    void ResetRace();
    RaceStatistics GetCurrentStatistics();
}
