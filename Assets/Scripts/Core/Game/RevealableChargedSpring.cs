using UnityEngine;
using TarodevController.old;

public class RevealableChargedSpring : BaseRevealableBlock
{
    [Header("Config")]
    [SerializeField] private ChargedSpringConfig _config;

    [Header("Compression Visual (Optional)")]
    [SerializeField] private Transform _compressionVisual;
    [SerializeField] private float _maxCompression = 0.3f;

    private bool _wasInteracting;
    private Vector3 _compressionOriginalScale;

    protected override void Awake()
    {
        base.Awake();

        if (_solidCollider != null)
        {
            _solidCollider.enabled = true;
            _solidCollider.isTrigger = false;
        }

        if (_compressionVisual != null)
        {
            _compressionOriginalScale = _compressionVisual.localScale;
        }
    }

    protected override void Update()
    {
        bool isInteracting = IsInteracting();
        base.Update();

        if (isInteracting)
        {
            ApplyCompressionVisual(GetStageProgress(), GetStageIndex());
        }
        else if (_wasInteracting)
        {
            ReleaseSpring(GetStageIndex());
        }

        _wasInteracting = isInteracting;
    }

    protected override void UpdateSolidCollider()
    {
        if (_solidCollider != null && !_solidCollider.enabled)
        {
            _solidCollider.enabled = true;
        }
    }

    protected override void UpdateVisuals(float alpha)
    {
        if (_config == null || _config.StageGradients == null || _config.StageGradients.Length == 0)
        {
            base.UpdateVisuals(alpha);
            return;
        }

        int stageIndex = GetStageIndex();
        int safeIndex = Mathf.Clamp(stageIndex, 0, _config.StageGradients.Length - 1);
        float stageProgress = GetStageProgress();

        Color c = _config.StageGradients[safeIndex].Evaluate(stageProgress);

        _renderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor(_colorPropertyID, c);
        _renderer.SetPropertyBlock(_propBlock);
    }

    private bool IsInteracting()
    {
        return _isBrushInside && _brushTransform != null;
    }

    private int GetStageCount()
    {
        if (_config == null) return 1;
        return Mathf.Max(1, _config.StageCount);
    }

    private int GetStageIndex()
    {
        int stageCount = GetStageCount();
        float stageSize = 1f / stageCount;
        int index = Mathf.FloorToInt(_currentAlpha / stageSize);
        return Mathf.Clamp(index, 0, stageCount - 1);
    }

    private float GetStageProgress()
    {
        int stageCount = GetStageCount();
        float stageSize = 1f / stageCount;
        int index = GetStageIndex();
        float start = index * stageSize;
        float end = start + stageSize;
        return Mathf.InverseLerp(start, end, _currentAlpha);
    }

    private void ReleaseSpring(int stageIndex)
    {
        if (_config == null || _config.StageReleaseForces == null || _config.StageReleaseForces.Length == 0) return;

        int forceIndex = Mathf.Clamp(stageIndex, 0, _config.StageReleaseForces.Length - 1);
        float force = _config.StageReleaseForces[forceIndex];
        if (force <= 0f) return;

        ApplyForceToOverlaps(force);
    }

    private void ApplyForceToOverlaps(float force)
    {
        if (_solidCollider == null) return;

        Bounds b = _solidCollider.bounds;
        Collider[] hits = Physics.OverlapBox(
            b.center,
            b.extents,
            _solidCollider.transform.rotation,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        Vector3 dir = transform.up;
        foreach (Collider hit in hits)
        {
            if (hit.TryGetComponent(out PlayerController0 controller))
            {
                controller.ApplySpringImpulse(
                    dir,
                    force,
                    _config.MaxUpSpeed,
                    _config.UpAcceleration,
                    _config.AssistDuration,
                    _config.UngroundTime,
                    true
                );
                continue;
            }

            if (hit.attachedRigidbody == null) continue;
            hit.attachedRigidbody.AddForce(dir * force, ForceMode.VelocityChange);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_config == null) return;
        if (_currentAlpha > 0.01f) return;

        Vector3 dir = transform.up;

        if (collision.collider.TryGetComponent(out PlayerController0 controller))
        {
            controller.ApplySpringImpulse(
                dir,
                _config.InactiveBounceForce,
                _config.MaxUpSpeed,
                _config.UpAcceleration,
                _config.AssistDuration,
                _config.UngroundTime,
                true
            );
            return;
        }

        Rigidbody rb = collision.rigidbody;
        if (rb == null) return;
        rb.AddForce(dir * _config.InactiveBounceForce, ForceMode.VelocityChange);
    }

    protected virtual void ApplyCompressionVisual(float stageProgress, int stageIndex)
    {
        if (_compressionVisual == null) return;
        float compression = Mathf.Clamp01(stageProgress) * _maxCompression;
        Vector3 scale = _compressionOriginalScale;
        scale.y = Mathf.Max(0.01f, scale.y * (1f - compression));
        _compressionVisual.localScale = scale;
    }
}
