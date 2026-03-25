using UnityEngine;

public class MultiDisplayActivator : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    void Start()
    {
        // Активируем второй дисплей
        if (Display.displays.Length > 1)
        {
            Display.displays[1].Activate();
            Debug.Log("Display 2 activated successfully");
            _camera.targetDisplay = 2;
        }
        else
        {
            Debug.LogWarning("Second display not found!");
        }
    }
}