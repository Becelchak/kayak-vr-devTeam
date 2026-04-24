using System;
using UnityEngine;
using UnityEngine.UIElements;

public class FastUIUpdater : MonoBehaviour
{
    private Label speed_label;

    void Start()
    {
        BuoyancyINFO.VelocityInfoUpdated += speedUpdate;

        var root = GetComponent<UIDocument>().rootVisualElement;
        speed_label = root.Q<Label>("speed");
    }

    private void speedUpdate(Vector3 vector)
    {
        speed_label.text = $"{vector.magnitude:F1}";
    }

    void OnDestroy()
    {
        
    }
}
