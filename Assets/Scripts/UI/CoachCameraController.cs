using UnityEngine;

public class CoachCameraController : MonoBehaviour
{
    [Header("Режимы")]
    public Transform target;            // цель для слежения (каяк)
    public bool followMode = true;       // начальный режим
    public Vector3 followOffset = new Vector3(5f, 2f, 0f); // смещение относительно цели

    [Header("Скорости")]
    public float moveSpeed = 10f;
    public float lookSpeed = 2f;
    public float fastSpeedMultiplier = 2f;   // ускорение при Shift
    public float followLerpSpeed = 5f;        // плавность следования

    [Header("Ограничения поворота (FreeFly)")]
    public float minPitch = -80f;
    public float maxPitch = 80f;

    private float pitch = 0f;
    private float yaw = 0f;
    private bool freeFlyMode = false;

    void Start()
    {
        // Инициализация
        if (followMode)
        {
            EnterFollowMode();
        }
        else
        {
            EnterFreeFlyMode();
        }
    }

    void Update()
    {
        // Переключение режимов по клавише F
        if (Input.GetKeyDown(KeyCode.F))
        {
            ToggleMode();
        }

        if (freeFlyMode)
        {
            UpdateFreeFly();
        }
        else
        {
            UpdateFollow();
        }
    }

    void ToggleMode()
    {
        if (freeFlyMode)
            EnterFollowMode();
        else
            EnterFreeFlyMode();
    }

    void EnterFreeFlyMode()
    {
        freeFlyMode = true;
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
        // Скрыть и заблокировать курсор для удобства
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void EnterFollowMode()
    {
        freeFlyMode = false;
        // Показать курсор
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void UpdateFreeFly()
    {
        // Поворот мышью
        float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

        // Перемещение
        float speed = moveSpeed;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            speed *= fastSpeedMultiplier;

        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) move += transform.forward;
        if (Input.GetKey(KeyCode.S)) move -= transform.forward;
        if (Input.GetKey(KeyCode.A)) move -= transform.right;
        if (Input.GetKey(KeyCode.D)) move += transform.right;
        if (Input.GetKey(KeyCode.E)) move += transform.up;
        if (Input.GetKey(KeyCode.Q)) move -= transform.up;

        transform.position += move.normalized * speed * Time.deltaTime;
    }

    void UpdateFollow()
    {
        if (target == null) return;

        // Желаемая позиция — цель + смещение в локальных координатах цели?
        // Проще: смещение в мировых координатах, но с учётом направления цели.
        // Например, камера всегда смотрит на цель с фиксированной стороны.
        Vector3 desiredPosition = target.position + followOffset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followLerpSpeed * Time.deltaTime);
        transform.LookAt(target);
    }
}