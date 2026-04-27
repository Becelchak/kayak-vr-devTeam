using UnityEngine;
using Crest;

[RequireComponent(typeof(Rigidbody))]
public class DoublePaddleSystem : MonoBehaviour
{
    [System.Serializable]
    public class Blade
    {
        public Transform bladeRoot;
        public Transform bladeTip;
        [HideInInspector] public Vector3 constrainedPosition;
    }

    [Header("Controllers")]
    public Transform leftController;
    public Transform rightController;

    [Header("Paddle")]
    public Transform doublePaddle;

    [Header("Blades")]
    public Blade leftBlade;
    public Blade rightBlade;

    [Header("Hand Constraints")]
    [Tooltip("Минимальное расстояние между руками для активации весла")]
    public float minHandDistance = 0.4f;
    [Tooltip("Максимальное допустимое расстояние между руками")]
    public float maxHandDistance = 1.5f;

    [Header("Physics")]
    public float bladeDepthThreshold = -0.05f;
    public float maxEffectiveSpeed = 2.5f;
    public float forceMultiplier = 60f;
    public float recoveryDrag = 0.5f;
    public float minEfficiency = 0.1f;
    public float maxEfficiency = 1.0f;

    [Header("Collision Prevention")]
    [Tooltip("Слои с которыми весло НЕ должно проходить (каяк и т.д.)")]
    public LayerMask blockingLayers;
    [Tooltip("Радиус проверки столкновений для лопастей")]
    public float bladeCollisionRadius = 0.08f;
    [Tooltip("Радиус проверки столкновений для стержня весла")]
    public float shaftCollisionRadius = 0.03f;
    [Tooltip("Количество точек проверки на стержне весла")]
    public int shaftCheckPoints = 5;

    [Header("Crest Sampling")]
    public float minSpatialLength = 1f;

    [Header("Debug")]
    public bool showDebugInfo = true;

    private Rigidbody rb;
    private Vector3 lastLeftTip, lastRightTip;
    private bool leftInWater, rightInWater;
    private bool isPaddleActive = false;

    private SampleHeightHelper _leftHeightHelper, _rightHeightHelper;
    private SampleFlowHelper _leftFlowHelper, _rightFlowHelper;
    private bool _initialized = false;

    // Для ограничения позиции весла
    private Vector3 constrainedLeftPos, constrainedRightPos;
    // Для детекции длины гребка
    // События для лопастей (можно сделать статическими или instance)
    public static event System.Action OnLeftBladeEnterWater;
    public static event System.Action OnLeftBladeExitWater;
    public static event System.Action OnRightBladeEnterWater;
    public static event System.Action OnRightBladeExitWater;

    // Флаги предыдущего состояния погружения
    private bool leftBladeWasInWater = false;
    private bool rightBladeWasInWater = false;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        if (leftBlade?.bladeTip != null) lastLeftTip = leftBlade.bladeTip.position;
        if (rightBlade?.bladeTip != null) lastRightTip = rightBlade.bladeTip.position;

        constrainedLeftPos = leftController != null ? leftController.position : Vector3.zero;
        constrainedRightPos = rightController != null ? rightController.position : Vector3.zero;
    }

    void LateUpdate()
    {
        if (leftController == null || rightController == null || doublePaddle == null) return;

        // Проверяем столкновения и ограничиваем позицию
        constrainedLeftPos = GetConstrainedPosition(leftController.position, constrainedLeftPos);
        constrainedRightPos = GetConstrainedPosition(rightController.position, constrainedRightPos);

        // Проверяем столкновение стержня весла
        if (CheckShaftCollision(constrainedLeftPos, constrainedRightPos))
        {
            // Если стержень проходит через каяк, возвращаем предыдущие позиции
            constrainedLeftPos = leftBlade.constrainedPosition;
            constrainedRightPos = rightBlade.constrainedPosition;
        }

        // Сохраняем текущие позиции
        leftBlade.constrainedPosition = constrainedLeftPos;
        rightBlade.constrainedPosition = constrainedRightPos;

        float handDistance = Vector3.Distance(constrainedLeftPos, constrainedRightPos);

        // Проверка минимального расстояния между руками
        isPaddleActive = handDistance >= minHandDistance && handDistance <= maxHandDistance;

        // Весло следует за ограниченными позициями
        doublePaddle.position = (constrainedLeftPos + constrainedRightPos) * 0.5f;

        Vector3 forward = constrainedRightPos - constrainedLeftPos;
        if (forward.magnitude > 0.01f)
        {
            doublePaddle.rotation = Quaternion.LookRotation(forward, Vector3.up);
        }

        if (!isPaddleActive && showDebugInfo)
        {
            //Debug.Log($"Весло неактивно! Расстояние между руками: {handDistance:F2}m (мин: {minHandDistance}m, макс: {maxHandDistance}m)");
        }
    }

    Vector3 GetConstrainedPosition(Vector3 targetPos, Vector3 currentPos)
    {
        // Проверяем можно ли переместиться в новую позицию
        Vector3 direction = targetPos - currentPos;
        float distance = direction.magnitude;

        if (distance < 0.001f) return currentPos;

        // Проверяем путь от текущей до целевой позиции
        RaycastHit hit;
        if (Physics.SphereCast(currentPos, bladeCollisionRadius, direction.normalized, out hit, distance, blockingLayers))
        {
            // Столкновение! Останавливаем на границе
            Vector3 constrainedPos = currentPos + direction.normalized * Mathf.Max(0, hit.distance - bladeCollisionRadius * 0.5f);

            if (showDebugInfo)
            {
                Debug.DrawLine(currentPos, hit.point, Color.red, 0.1f);
            }

            return constrainedPos;
        }

        // Дополнительная проверка целевой позиции
        if (Physics.CheckSphere(targetPos, bladeCollisionRadius, blockingLayers))
        {
            if (showDebugInfo)
            {
                Debug.DrawLine(currentPos, targetPos, Color.yellow, 0.1f);
            }
            return currentPos; // Оставляем на месте
        }

        return targetPos;
    }

    bool CheckShaftCollision(Vector3 leftPos, Vector3 rightPos)
    {
        // Проверяем стержень весла между лопастями
        for (int i = 0; i < shaftCheckPoints; i++)
        {
            float t = i / (float)(shaftCheckPoints - 1);
            Vector3 checkPoint = Vector3.Lerp(leftPos, rightPos, t);

            if (Physics.CheckSphere(checkPoint, shaftCollisionRadius, blockingLayers))
            {
                if (showDebugInfo)
                {
                    Debug.DrawLine(checkPoint, checkPoint + Vector3.up * 0.1f, Color.magenta, 0.1f);
                }
                return true;
            }
        }
        return false;
    }

    void FixedUpdate()
    {
        if (OceanRenderer.Instance == null) return;

        if (!_initialized)
        {
            _leftHeightHelper = new SampleHeightHelper();
            _rightHeightHelper = new SampleHeightHelper();
            _leftFlowHelper = new SampleFlowHelper();
            _rightFlowHelper = new SampleFlowHelper();
            _initialized = true;
        }

        // Применяем силу только если весло активно
        if (!isPaddleActive)
        {
            leftInWater = false;
            rightInWater = false;
            return;
        }

        if (leftBlade?.bladeTip != null)
            ProcessBlade(leftBlade, ref lastLeftTip, ref leftInWater, _leftHeightHelper, _leftFlowHelper, true);

        if (rightBlade?.bladeTip != null)
            ProcessBlade(rightBlade, ref lastRightTip, ref rightInWater, _rightHeightHelper, _rightFlowHelper, false);
    }

    void ProcessBlade(Blade blade, ref Vector3 lastPos, ref bool inWater, SampleHeightHelper heightHelper, SampleFlowHelper flowHelper, bool isLeftBlade)
    {
        Transform tip = blade.bladeTip;
        Vector3 current = tip.position;
        Vector3 velocity = (current - lastPos) / Time.fixedDeltaTime;
        lastPos = current;

        // Если лопасть внутри блокирующего объекта, не применяем силу
        if (Physics.CheckSphere(current, bladeCollisionRadius, blockingLayers))
        {
            inWater = false;
            return;
        }

        heightHelper.Init(current, minSpatialLength);
        if (!heightHelper.Sample(out float waterHeight, out _, out Vector3 waterVelocity)) return;

        // Add flow to water velocity
        flowHelper.Init(current, minSpatialLength);
        if (flowHelper.Sample(out Vector2 flow2D))
        {
            waterVelocity += new Vector3(flow2D.x, 0f, flow2D.y);
        }

        bool currentlyInWater = current.y < waterHeight + bladeDepthThreshold;
        inWater = currentlyInWater;

        if (isLeftBlade)
        {
            if (currentlyInWater && !leftBladeWasInWater)
            {
                OnLeftBladeEnterWater?.Invoke();
                leftBladeWasInWater = true;
            }
            else if (!currentlyInWater && leftBladeWasInWater)
            {
                OnLeftBladeExitWater?.Invoke();
                leftBladeWasInWater = false;
            }
        }
        else
        {
            if (currentlyInWater && !rightBladeWasInWater)
            {
                OnRightBladeEnterWater?.Invoke();
                rightBladeWasInWater = true;
            }
            else if (!currentlyInWater && rightBladeWasInWater)
            {
                OnRightBladeExitWater?.Invoke();
                rightBladeWasInWater = false;
            }
        }

        if (currentlyInWater)
        {
            Vector3 relVel = velocity - waterVelocity;
            Vector3 localVel = transform.InverseTransformDirection(relVel);
            float angleEff = CalculateBladeEfficiency(blade.bladeRoot.up);

            // Дополнительное снижение эффективности если руки слишком близко
            float handDistance = Vector3.Distance(constrainedLeftPos, constrainedRightPos);
            float distanceEfficiency = Mathf.Clamp01((handDistance - minHandDistance) / (maxHandDistance - minHandDistance));
            angleEff *= distanceEfficiency;

            if (localVel.z < -0.1f) // Pulling back (effective stroke)
            {
                float speed = Mathf.Clamp(-localVel.z, 0f, maxEffectiveSpeed);
                Vector3 force = transform.forward * speed * forceMultiplier * angleEff;
                rb.AddForceAtPosition(force, current, ForceMode.Force);
            }
            else if (localVel.z > 0.1f) // Pushing forward (drag when submerged)
            {
                float drag = localVel.z * forceMultiplier * 0.3f * angleEff;
                rb.AddForceAtPosition(-transform.forward * drag, current, ForceMode.Force);
            }
        }
        else
        {
            // Air drag
            if (velocity.magnitude > 0.2f)
            {
                Vector3 airDrag = -velocity * recoveryDrag;
                airDrag.y = 0f;
                rb.AddForceAtPosition(airDrag, current, ForceMode.Force);
            }
        }
    }

    float CalculateBladeEfficiency(Vector3 normal)
    {
        float dot = Mathf.Abs(Vector3.Dot(normal, Vector3.up));
        return Mathf.Lerp(minEfficiency, maxEfficiency, dot * dot);
    }

    void OnDrawGizmos()
    {
        if (leftController != null && rightController != null)
        {
            float distance = Vector3.Distance(
                Application.isPlaying ? constrainedLeftPos : leftController.position,
                Application.isPlaying ? constrainedRightPos : rightController.position
            );

            // Рисуем линию между позициями рук
            Gizmos.color = isPaddleActive ? Color.green : Color.red;
            if (Application.isPlaying)
            {
                Gizmos.DrawLine(constrainedLeftPos, constrainedRightPos);

                // Линии от контроллеров к ограниченным позициям
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(leftController.position, constrainedLeftPos);
                Gizmos.DrawLine(rightController.position, constrainedRightPos);
            }
            else
            {
                Gizmos.DrawLine(leftController.position, rightController.position);
            }

            // Рисуем сферы минимального и максимального расстояния
            Vector3 leftPos = Application.isPlaying ? constrainedLeftPos : leftController.position;

            Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
            Gizmos.DrawWireSphere(leftPos, minHandDistance);

            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawWireSphere(leftPos, maxHandDistance);

            // Рисуем точки проверки на стержне
            if (Application.isPlaying)
            {
                Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
                for (int i = 0; i < shaftCheckPoints; i++)
                {
                    float t = i / (float)(shaftCheckPoints - 1);
                    Vector3 checkPoint = Vector3.Lerp(constrainedLeftPos, constrainedRightPos, t);
                    Gizmos.DrawWireSphere(checkPoint, shaftCollisionRadius);
                }
            }
        }

        if (OceanRenderer.Instance == null) return;
        DrawBlade(leftBlade, _leftHeightHelper);
        DrawBlade(rightBlade, _rightHeightHelper);
    }

    void DrawBlade(Blade blade, SampleHeightHelper helper)
    {
        if (blade?.bladeTip == null) return;

        Vector3 pos = blade.bladeTip.position;
        float waterHeight = OceanRenderer.Instance.SeaLevel;
        bool inWater = false;

        if (Application.isPlaying && helper != null)
        {
            helper.Init(pos, minSpatialLength);
            if (helper.Sample(out waterHeight, out _, out _))
            {
                inWater = pos.y < waterHeight + bladeDepthThreshold;
                Gizmos.color = new Color(0f, 0.5f, 1f, 0.5f);
                Gizmos.DrawLine(pos, new Vector3(pos.x, waterHeight, pos.z));
            }
        }
        else
        {
            inWater = pos.y < waterHeight + bladeDepthThreshold;
        }

        Gizmos.color = inWater ? Color.cyan : Color.yellow;
        Gizmos.DrawSphere(pos, 0.04f);

        // Рисуем радиус проверки столкновений
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawWireSphere(pos, bladeCollisionRadius);
    }

    void OnValidate()
    {
        if (minHandDistance >= maxHandDistance)
        {
            Debug.LogWarning("minHandDistance должно быть меньше maxHandDistance!");
            minHandDistance = maxHandDistance - 0.1f;
        }
    }
}