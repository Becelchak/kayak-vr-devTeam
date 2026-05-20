using UnityEngine;

public class RaceEndTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var race = (RaceService)ServiceLocator.Instance.GetService<IRaceService>();
            race.FinishRace();
        }
    }
}
