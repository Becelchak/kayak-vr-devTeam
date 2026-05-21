using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Ёкраны UI (меню)")]
    [SerializeField] private GameObject startScreen;
    [SerializeField] private GameObject routeScreen;

    [Header("—истемы")]
    [SerializeField] private GameObject athleteSystem;   // XR Origin + Kayak
    [SerializeField] private GameObject coachSystem;     // Coach PC + дашборд

    private string selectedRole;

    private void Start()
    {
        ShowStartScreen();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ShowStartScreen()
    {
        if (startScreen != null) startScreen.SetActive(true);
        if (routeScreen != null) routeScreen.SetActive(false);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void ShowRouteScreen()
    {
        if (startScreen != null) startScreen.SetActive(false);
        if (routeScreen != null) routeScreen.SetActive(true);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void SelectAthlete()
    {
        selectedRole = "athlete";
        ShowRouteScreen();
    }

    public void SelectCoach()
    {
        selectedRole = "coach";
        ShowRouteScreen();
    }

    public void SelectObserver()
    {
        selectedRole = "observer";
        ShowRouteScreen();
    }

    public void StartGame()
    {
        // —крываем UI меню
        if (startScreen != null) startScreen.SetActive(false);
        if (routeScreen != null) routeScreen.SetActive(false);

        // ¬ключаем/выключаем системы
        switch (selectedRole)
        {
            case "athlete":
                if (athleteSystem != null) athleteSystem.SetActive(true);
                if (coachSystem != null) coachSystem.SetActive(false);
                break;
            case "coach":
            case "observer":
                if (athleteSystem != null) athleteSystem.SetActive(false);
                if (coachSystem != null) coachSystem.SetActive(true);
                break;
        }

        //  урсор
        if (selectedRole == "athlete")
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        else
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public void ReturnToMenu()
    {
        ShowStartScreen();
    }
}