using UnityEngine;

public class RaceStartTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var race = (RaceService)ServiceLocator.Instance.GetService<IRaceService>();
            race.StartRace();
        }
    }
}