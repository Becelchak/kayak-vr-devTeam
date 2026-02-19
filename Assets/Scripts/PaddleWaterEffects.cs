using UnityEngine;
using Crest;

public class PaddleWaterEffects : MonoBehaviour
{
    [Header("Кончики лопастей")]
    public Transform leftBladeTip;
    public Transform rightBladeTip;

    [Header("SphereWaterInteraction — рябь")]
    public SphereWaterInteraction leftRipple;
    public SphereWaterInteraction rightRipple;

    [Header("Настройки")]
    public float bladeDepthThreshold = -0.05f;
    public float minVelocityForEffect = 0.3f;

    [UnityEngine.Range(10f, 100f)]
    public float rippleWeightMin = 20f;

    [UnityEngine.Range(10f, 100f)]
    public float rippleWeightMax = 60f;

    [UnityEngine.Range(1f, 10f)]
    public float maxBladeSpeed = 3f;

    [Header("Crest Sampling")]
    public float minSpatialLength = 1f;

    private SampleHeightHelper _leftHelper = new SampleHeightHelper();
    private SampleHeightHelper _rightHelper = new SampleHeightHelper();

    private Vector3 _lastLeftPos, _lastRightPos;

    void Start()
    {
        if (leftBladeTip) _lastLeftPos = leftBladeTip.position;
        if (rightBladeTip) _lastRightPos = rightBladeTip.position;

        if (leftRipple) leftRipple.enabled = false;
        if (rightRipple) rightRipple.enabled = false;
    }

    void FixedUpdate()
    {
        if (OceanRenderer.Instance == null) return;

        ProcessBlade(leftBladeTip, ref _lastLeftPos, _leftHelper, leftRipple);
        ProcessBlade(rightBladeTip, ref _lastRightPos, _rightHelper, rightRipple);
    }

    void ProcessBlade(Transform tip, ref Vector3 lastPos,
                      SampleHeightHelper helper, SphereWaterInteraction ripple)
    {
        if (tip == null || ripple == null) return;

        Vector3 pos = tip.position;
        float speed = (pos - lastPos).magnitude / Time.fixedDeltaTime;
        lastPos = pos;

        helper.Init(pos, minSpatialLength);
        bool sampled = helper.Sample(out float waterHeight, out _, out _);
        bool inWater = sampled && pos.y < waterHeight + bladeDepthThreshold;

        if (inWater && speed > minVelocityForEffect)
        {
            float t = Mathf.Clamp01(speed / maxBladeSpeed);
            ripple.enabled = true;
            ripple._weight = Mathf.Lerp(rippleWeightMin, rippleWeightMax, t);
        }
        else
        {
            ripple.enabled = false;
        }
    }
}