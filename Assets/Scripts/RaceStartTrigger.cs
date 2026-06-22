using UnityEngine;

public class RaceStartTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var race = (RaceService)ServiceLocator.Instance.GetService<IRaceService>();
            Debug.Log("Enter");
            race.StartRace();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var race = (RaceService)ServiceLocator.Instance.GetService<IRaceService>();
            if(race.IsRaceActive)
                return;
            Debug.Log("Stay");
            race.StartRace();
        }
    }
}