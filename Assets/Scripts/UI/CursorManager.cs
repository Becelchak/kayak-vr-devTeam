using UnityEngine;

public class CursorManager : MonoBehaviour
{
    private void Start()
    {
        // Показываем и разблокируем курсор для UI меню
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    // Этот метод можно вызывать, когда игра начинается (например, при старте геймплея)
    public void LockCursorForGameplay()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Вернуть курсор для меню
    public void UnlockCursorForMenu()
    {
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }
}