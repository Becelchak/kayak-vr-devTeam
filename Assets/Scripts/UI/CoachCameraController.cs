using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Управление камерой тренера: режимы свободного полёта и слежения за каяком.
/// Поддерживает блокировку управления при активных UI-элементах.
/// </summary>
public class CoachCameraController : MonoBehaviour
{
    [Header("Режимы")]
    [Tooltip("Цель для слежения (каяк)")]
    public Transform target;
    [Tooltip("Начальный режим (true = слежение, false = свободный полёт)")]
    public bool followMode = true;
    [Tooltip("Смещение камеры относительно цели в режиме слежения")]
    public Vector3 followOffset = new Vector3(5f, 2f, 0f);

    [Header("Скорости")]
    [Tooltip("Скорость перемещения в свободном режиме")]
    public float moveSpeed = 10f;
    [Tooltip("Чувствительность поворота мыши")]
    public float lookSpeed = 2f;
    [Tooltip("Множитель скорости при зажатом Shift")]
    public float fastSpeedMultiplier = 2f;
    [Tooltip("Скорость интерполяции в режиме слежения")]
    public float followLerpSpeed = 5f;

    [Header("Ограничения поворота (FreeFly)")]
    [Tooltip("Минимальный угол наклона камеры вниз")]
    public float minPitch = -80f;
    [Tooltip("Максимальный угол наклона камеры вверх")]
    public float maxPitch = 80f;

    [Header("Управление")]
    // Статический счётчик активных UI-элементов, блокирующих камеру
    private static int _uiLockCount = 0;
    private bool IsUILocked => _uiLockCount > 0;

    private float pitch = 0f;
    private float yaw = 0f;
    private float currentAngleX = 0f;
    private float currentAngleY = 20f;
    private bool freeFlyMode = false;

    void Start()
    {
        if (followMode)
        {
            EnterFollowMode();
        }
        else
        {
            EnterFreeFlyMode();
        }
    }

    /// <summary>
    /// Вызывается UI-элементом при открытии (OnEnable)
    /// </summary>
    public static void AddUILock()
    {
        _uiLockCount++;
    }

    /// <summary>
    /// Вызывается UI-элементом при закрытии (OnDisable/OnDestroy)
    /// </summary>
    public static void RemoveUILock()
    {
        if (_uiLockCount > 0) _uiLockCount--;
    }

    void Update()
    {
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

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void EnterFollowMode()
    {
        freeFlyMode = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    void UpdateFreeFly()
    {
        if (IsUILocked) return;

        float mouseX = Input.GetAxis("Mouse X") * lookSpeed;
        float mouseY = Input.GetAxis("Mouse Y") * lookSpeed;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
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
        if (target == null || IsUILocked) return;

        currentAngleX += Input.GetAxis("Mouse X") * lookSpeed;
        currentAngleY -= Input.GetAxis("Mouse Y") * lookSpeed;
        currentAngleY = Mathf.Clamp(currentAngleY, -80f, 80f);

        Quaternion rotation = Quaternion.Euler(currentAngleY, currentAngleX, 0);
        Vector3 desiredPosition = target.position + rotation * followOffset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, followLerpSpeed * Time.deltaTime);
        transform.LookAt(target);
    }
}