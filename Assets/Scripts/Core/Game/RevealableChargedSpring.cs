using UnityEngine;
using TarodevController.old;

public class RevealableChargedSpring : BaseRevealableBlock
{
    [Header("Config")]
    [SerializeField] private ChargedSpringConfig _config;

    [Header("Compression Model")]
    [SerializeField] private Transform _compressionRoot;
    [SerializeField] private Collider _releaseTrigger;

    private bool _wasInteracting;
    private Vector3 _compressionOriginalScale;
    private float _chargeTime;
    private float _lastCompressionRatio;
    private float _pendingReleaseForce;
    private float _pendingReleaseTime;

    protected override void Awake()
    {
        base.Awake();

        if (_solidCollider != null)
        {
            _solidCollider.enabled = true;
            _solidCollider.isTrigger = false;
        }

        if (_compressionRoot == null)
        {
            _compressionRoot = transform;
        }

        _compressionOriginalScale = _compressionRoot.localScale;
    }

    protected override void Update()
    {
        base.Update();

        bool isInteracting = IsInteracting();
        bool releasedThisFrame = _wasInteracting && !isInteracting;

        UpdateChargeTime(isInteracting);
        if (isInteracting)
        {
            _lastCompressionRatio = GetCompressionRatio();
        }

        if (releasedThisFrame)
        {
            ReleaseSpring(_lastCompressionRatio);
        }

        UpdateCompressionVisual(isInteracting, _lastCompressionRatio);
        TryApplyPendingRelease();

        _wasInteracting = isInteracting;
    }

    protected override void UpdateSolidCollider()
    {
        if (_solidCollider != null && !_solidCollider.enabled)
        {
            _solidCollider.enabled = true;
        }
    }

    private bool IsInteracting()
    {
        return _isBrushInside && _brushTransform != null;
    }

    private void ReleaseSpring(float compressionRatio)
    {
        if (_config == null) return;

        float compressionAmount = Mathf.Clamp01(compressionRatio) * Mathf.Clamp01(_config.MaxCompressionRatio);
        float releaseValue = _config.ForceCoefficient * compressionAmount;
        if (_config.DebugLog)
        {
            Debug.Log($"[ChargedSpring] Release: ratio={compressionRatio:F2}, amount={compressionAmount:F2}, value={releaseValue:F2}", this);
        }
        if (releaseValue <= 0f) return;

        if (!ApplyReleaseToTriggerArea(releaseValue, transform.up))
        {
            _pendingReleaseForce = releaseValue;
            _pendingReleaseTime = Mathf.Max(_pendingReleaseTime, _config.ReleaseWindowTime);
            if (_config.DebugLog)
            {
                Debug.Log($"[ChargedSpring] No target found, start window {_pendingReleaseTime:F2}s", this);
            }
        }
    }

    private bool ApplyReleaseToTriggerArea(float value, Vector3 direction)
    {
        Collider trigger = _releaseTrigger != null ? _releaseTrigger : _solidCollider;
        if (trigger == null) return false;

        Bounds b = trigger.bounds;
        Collider[] hits = Physics.OverlapBox(
            b.center,
            b.extents,
            trigger.transform.rotation,
            ~0,
            QueryTriggerInteraction.Collide
        );

        bool appliedAny = false;
        foreach (Collider hit in hits)
        {
            if (!TryGetPlayerTarget(hit, out PlayerController0 controller)) continue;
            ApplyImpulseToTarget(hit, controller, direction, value);
            appliedAny = true;
        }

        if (_config != null && _config.DebugLog)
        {
            Debug.Log($"[ChargedSpring] Trigger hits={hits.Length}, applied={appliedAny}", this);
        }
        return appliedAny;
    }

    private void ApplyImpulseToTarget(Collider target, PlayerController0 controller, Vector3 direction, float value)
    {
        if (target == null) return;
        if (_config == null) return;

        Vector3 dir = direction;
        dir.z = 0f;
        dir = dir.sqrMagnitude > 0f ? dir.normalized : Vector3.up;
        if (controller != null)
        {
            controller.ZeroZVelocity();
            if (_config.DebugLog)
            {
                Debug.Log($"[ChargedSpring] Apply to PlayerController0 mode={_config.ReleaseForceMode} value={value:F2}", controller);
            }
            if (_config.ReleaseForceMode == ChargedSpringConfig.ReleaseMode.SetVelocity)
            {
                controller.SetVelocityAlongDirection(dir, value, _config.UngroundTime);
            }
            else
            {
                controller.ApplySpringImpulse(
                    dir,
                    value,
                    _config.MaxUpSpeed,
                    _config.UpAcceleration,
                    _config.AssistDuration,
                    _config.UngroundTime,
                    true
                );
            }
            return;
        }

        Rigidbody rb = target.attachedRigidbody;
        if (rb == null) return;
        Vector3 rbVelocity = rb.linearVelocity;
        rbVelocity.z = 0f;
        rb.linearVelocity = rbVelocity;

        if (_config.ReleaseForceMode == ChargedSpringConfig.ReleaseMode.SetVelocity)
        {
            float current = Vector3.Dot(rb.linearVelocity, dir);
            rb.linearVelocity += dir * (value - current);
            if (_config.DebugLog)
            {
                Debug.Log($"[ChargedSpring] Apply to Rigidbody setVel value={value:F2}", rb);
            }
        }
        else
        {
            rb.AddForce(dir * value, ForceMode.Impulse);
            if (_config.DebugLog)
            {
                Debug.Log($"[ChargedSpring] Apply to Rigidbody impulse value={value:F2}", rb);
            }
        }
    }

    private bool TryGetPlayerTarget(Collider hit, out PlayerController0 controller)
    {
        controller = hit.GetComponentInParent<PlayerController0>();
        if (controller != null) return true;

        if (hit.CompareTag("Player")) return true;
        Transform root = hit.transform.root;
        return root != null && root.CompareTag("Player");
    }

    private void UpdateCompressionVisual(bool isInteracting, float compressionRatio)
    {
        if (_compressionRoot == null || _config == null) return;

        Vector3 targetScale = _compressionOriginalScale;
        if (isInteracting)
        {
            float compressionAmount = Mathf.Clamp01(compressionRatio) * Mathf.Clamp01(_config.MaxCompressionRatio);
            targetScale.y = Mathf.Max(0.01f, _compressionOriginalScale.y * (1f - compressionAmount));
            _compressionRoot.localScale = targetScale;
            return;
        }

        float speed = Mathf.Max(0f, _config.VisualReturnSpeed);
        _compressionRoot.localScale = Vector3.MoveTowards(
            _compressionRoot.localScale,
            _compressionOriginalScale,
            speed * Time.deltaTime
        );
    }

    private void TryApplyPendingRelease()
    {
        if (_pendingReleaseTime <= 0f) return;

        _pendingReleaseTime -= Time.deltaTime;
        if (ApplyReleaseToTriggerArea(_pendingReleaseForce, transform.up))
        {
            _pendingReleaseTime = 0f;
            _pendingReleaseForce = 0f;
            if (_config != null && _config.DebugLog)
            {
                Debug.Log("[ChargedSpring] Pending release applied", this);
            }
        }
    }

    private void UpdateChargeTime(bool isInteracting)
    {
        if (!isInteracting)
        {
            _chargeTime = 0f;
            return;
        }

        _chargeTime += Time.deltaTime;
    }

    private float GetCompressionRatio()
    {
        if (_config == null) return 0f;
        if (_config.CompressToMaxTime <= 0f) return 1f;
        return Mathf.Clamp01(_chargeTime / _config.CompressToMaxTime);
    }
}
