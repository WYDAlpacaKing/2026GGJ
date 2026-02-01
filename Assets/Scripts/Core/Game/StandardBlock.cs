using UnityEngine;
using DG.Tweening;
using Alpaca.Game.Audio;


public class StandardBlock : BaseRevealableBlock
{
    [Header("BaseMap 透明度 (呼吸)")]
    [Tooltip("需要做呼吸透明度的 MeshRenderer（3 个）")]
    [SerializeField] private MeshRenderer[] _baseMapRenderers = new MeshRenderer[3];
    [Tooltip("BaseMap 颜色属性名")]
    [SerializeField] private string _baseMapPropertyName = "_BaseColor";
    [Tooltip("呼吸最低透明度")]
    [SerializeField] private float _baseMapAlphaMin = 0.2f;
    [Tooltip("呼吸最高透明度")]
    [SerializeField] private float _baseMapAlphaMax = 0.9f;
    [Tooltip("呼吸周期时长")]
    [SerializeField] private float _breatheDuration = 1.5f;
    [Tooltip("回正后从 0 过渡到最低透明度的时长")]
    [SerializeField] private float _returnToBreathDuration = 0.4f;

    [Header("Emission (Material)")]
    [Tooltip("需要调节发光的 Renderer")]
    [SerializeField] private Renderer _emissionRenderer;
    [Tooltip("Emission 颜色属性名")]
    [SerializeField] private string _emissionPropertyName = "_EmissionColor";
    [Tooltip("Emission 颜色（与强度相乘）")]
    [SerializeField] private Color _emissionColor = Color.white;
    [Tooltip("激活中 Emission 强度起始值")]
    [SerializeField] private float _emissionIntensityFrom = 0f;
    [Tooltip("激活中 Emission 强度结束值")]
    [SerializeField] private float _emissionIntensityTo = 2f;

    [Header("果实 (GameObject)")]
    [Tooltip("果实 GameObject")]
    [SerializeField] private GameObject _fruitObject;
    [Tooltip("果实缩放目标（可选，避免缩放父物体导致平台抖动）")]
    [SerializeField] private Transform _fruitScaleTarget;
    [Tooltip("是否用当前模型的原始尺寸作为基准，再用倍率缩放")]
    [SerializeField] private bool _useRelativeFruitScale = true;
    [Tooltip("使用相对缩放时：最小倍率")]
    [SerializeField] private Vector3 _fruitScaleSmallMultiplier = new Vector3(0.6f, 0.6f, 0.6f);
    [Tooltip("使用相对缩放时：略微超过正常尺寸的倍率")]
    [SerializeField] private Vector3 _fruitScaleOvershootMultiplier = new Vector3(1.1f, 1.1f, 1.1f);
    [Tooltip("使用绝对缩放时：最小尺寸")]
    [SerializeField] private Vector3 _fruitScaleSmall = Vector3.one * 0.3f;
    [Tooltip("使用绝对缩放时：略微超过正常尺寸")]
    [SerializeField] private Vector3 _fruitScaleOvershoot = Vector3.one * 1.2f;
    [Tooltip("果实长大（小->超出）的时长")]
    [SerializeField] private float _fruitGrowDuration = 0.25f;
    [Tooltip("果实回到正常尺寸的时长")]
    [SerializeField] private float _fruitSettleDuration = 0.2f;

    [Header("Particles Scale")]
    [Tooltip("粒子效果 Transform（4 个）")]
    [SerializeField] private Transform[] _particleEffects = new Transform[4];
    [Tooltip("粒子缩放起始值")]
    [SerializeField] private Vector3 _particleScaleFrom = Vector3.one * 0.3f;
    [Tooltip("粒子缩放目标值")]
    [SerializeField] private Vector3 _particleScaleTo = Vector3.one;
    [Tooltip("粒子缩放动画时长")]
    [SerializeField] private float _particleScaleDuration = 0.3f;
    
    [Header("Debug")]
    [Tooltip("输出调试日志")]
    [SerializeField] private bool _debugLog;

    private MaterialPropertyBlock _baseMapBlock;
    private Color[] _baseMapColors;
    private Tween _breatheTween;
    private int _emissionPropertyId;
    private Sequence _activateSequence;
    private Sequence _deactivateSequence;
    private Transform _fruitTransform;
    private Vector3 _fruitScaleNormal;
    private Vector3 _fruitScaleSmallResolved;
    private Vector3 _fruitScaleOvershootResolved;
    private bool _activated;
    private bool _fruitIsSelf;
    private bool _fruitIsParent;
    private bool _isDeactivating;
    private float _lastLoggedAlpha = -1f;
    private AudioSource _flyLoopSource;

    protected override void Awake()
    {
        _applyBaseColor = false;
        InitVisuals();
        base.Awake();
    }

    protected override void OnFullyRevealed()
    {
        base.OnFullyRevealed();
        // AudioSource.PlayClipAtPoint(clip, transform.position);
    }

    protected override void UpdateVisuals(float alpha)
    {
        base.UpdateVisuals(alpha);

        if (_debugLog)
        {
            LogState(alpha);
        }

        UpdateEmissionIntensity(alpha);

        bool isInactive = alpha <= 0.01f;
        bool isActive = alpha >= 0.99f;

        if (isActive && !_activated)
        {
            StartActivateSequence();
        }
        else if (!isActive && _activated)
        {
            StartDeactivateSequence();
        }

        if (isInactive && !_activated && !_isDeactivating)
        {
            EnsureBreathing();
        }
        else if (!_activated && !_isDeactivating)
        {
            StopBreathing();
            ApplyBaseMapAlpha(_baseMapAlphaMax);
        }
    }

    private void InitVisuals()
    {
        _baseMapBlock = new MaterialPropertyBlock();
        CacheBaseMapColors();
        _emissionPropertyId = Shader.PropertyToID(_emissionPropertyName);
        if (_debugLog)
        {
            Debug.Log($"[StandardBlock] Init: baseMapRenderers={_baseMapRenderers?.Length ?? 0}, emissionRenderer={_emissionRenderer}, fruit={_fruitObject}, particles={_particleEffects?.Length ?? 0}", this);
        }

        if (_fruitObject != null)
        {
            _fruitIsSelf = _fruitObject == gameObject;
            _fruitIsParent = transform.IsChildOf(_fruitObject.transform);
            if (_fruitIsSelf)
            {
                Debug.LogWarning("[StandardBlock] Fruit Object is the same GameObject as this component; skipping fruit activation to avoid disabling this script.", this);
            }
            else if (_fruitIsParent)
            {
                Debug.LogWarning("[StandardBlock] Fruit Object is a parent of this platform. Its scaling may move the platform and cause jitter.", this);
            }
            _fruitTransform = _fruitScaleTarget != null ? _fruitScaleTarget : _fruitObject.transform;
            _fruitScaleNormal = _fruitTransform.localScale;
            ResolveFruitScales();
            if (CanToggleFruitObject()) _fruitObject.SetActive(false);
        }

        SetParticleScales(_particleScaleFrom);
        EnsureBreathing();
    }

    private void CacheBaseMapColors()
    {
        if (_baseMapRenderers == null || _baseMapRenderers.Length == 0)
        {
            _baseMapColors = null;
            return;
        }

        _baseMapColors = new Color[_baseMapRenderers.Length];
        for (int i = 0; i < _baseMapRenderers.Length; i++)
        {
            MeshRenderer renderer = _baseMapRenderers[i];
            if (renderer == null)
            {
                _baseMapColors[i] = Color.white;
                continue;
            }

            if (renderer.sharedMaterial != null && renderer.sharedMaterial.HasProperty(_baseMapPropertyName))
            {
                Debug.LogWarning("[StandardBlock] cchageBaseMap is already", this);
                _baseMapColors[i] = renderer.sharedMaterial.GetColor(_baseMapPropertyName);
            }
            else
            {
                _baseMapColors[i] = Color.white;
            }
        }
    }

    private void UpdateEmissionIntensity(float alpha)
    {
        if (_emissionRenderer == null) return;
        if (_baseMapBlock == null) _baseMapBlock = new MaterialPropertyBlock();

        float emissionIntensity = Mathf.Lerp(_emissionIntensityFrom, _emissionIntensityTo, alpha);
        _emissionRenderer.GetPropertyBlock(_baseMapBlock);
        _baseMapBlock.SetColor(_emissionPropertyId, _emissionColor * emissionIntensity);
        _emissionRenderer.SetPropertyBlock(_baseMapBlock);
    }

    private void EnsureBreathing()
    {
        if (_breatheTween != null && _breatheTween.IsActive()) return;
        ApplyBaseMapAlpha(_baseMapAlphaMin);

        _breatheTween = DOTween.To(
                () => 0f,
                t => ApplyBaseMapAlpha(Mathf.Lerp(_baseMapAlphaMin, _baseMapAlphaMax, t)),
                1f,
                Mathf.Max(0.01f, _breatheDuration)
            )
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(false);
    }

    private void StopBreathing()
    {
        if (_breatheTween == null) return;
        _breatheTween.Kill();
        _breatheTween = null;
    }

    private void ApplyBaseMapAlpha(float alpha)
    {
        if (_baseMapRenderers == null || _baseMapRenderers.Length == 0) return;
        if (_baseMapColors == null || _baseMapColors.Length != _baseMapRenderers.Length) return;

        for (int i = 0; i < _baseMapRenderers.Length; i++)
        {
            MeshRenderer renderer = _baseMapRenderers[i];
            if (renderer == null) continue;

            Color baseColor = _baseMapColors[i];
            baseColor.a = Mathf.Clamp01(alpha);
            renderer.GetPropertyBlock(_baseMapBlock);
            _baseMapBlock.SetColor(_baseMapPropertyName, baseColor);
            renderer.SetPropertyBlock(_baseMapBlock);
        }
    }

    private void StartActivateSequence()
    {
        _activated = true;
        StopBreathing();
        KillSequences();
        if (_debugLog) Debug.Log("[StandardBlock] Activate sequence start", this);

        if (_fruitTransform != null)
        {
            if (CanToggleFruitObject()) _fruitObject.SetActive(true);
            _fruitTransform.localScale = _fruitScaleSmallResolved;
        }

        SetParticleScales(_particleScaleFrom);
        ApplyBaseMapAlpha(0f);

        _activateSequence = DOTween.Sequence();

        if (_fruitTransform != null)
        {
            _activateSequence.Append(_fruitTransform.DOScale(_fruitScaleOvershootResolved, _fruitGrowDuration).SetEase(Ease.OutQuad));
            _activateSequence.Append(_fruitTransform.DOScale(_fruitScaleNormal, _fruitSettleDuration).SetEase(Ease.OutQuad));
        }

        _activateSequence.Append(AnimateParticles(_particleScaleTo));

        StartFlyLoopAudio();
    }

    private void StartDeactivateSequence()
    {
        _activated = false;
        _isDeactivating = true;
        KillSequences();
        if (_debugLog) Debug.Log("[StandardBlock] Deactivate sequence start", this);

        StopFlyLoopAudio();

        if (_fruitTransform != null)
        {
            if (CanToggleFruitObject()) _fruitObject.SetActive(true);
            _fruitTransform.localScale = _fruitScaleNormal;
        }

        _deactivateSequence = DOTween.Sequence();
        ApplyBaseMapAlpha(0f);
        _deactivateSequence.Append(AnimateParticles(_particleScaleFrom));

        if (_fruitTransform != null)
        {
            _deactivateSequence.Append(_fruitTransform.DOScale(_fruitScaleOvershootResolved, _fruitSettleDuration).SetEase(Ease.OutQuad));
            _deactivateSequence.Append(_fruitTransform.DOScale(_fruitScaleSmallResolved, _fruitGrowDuration).SetEase(Ease.OutQuad));
            _deactivateSequence.AppendCallback(() =>
            {
                ApplyBaseMapAlpha(0f);
                StopBreathing();
                DOTween.To(
                        () => 0f,
                        a => ApplyBaseMapAlpha(a),
                        _baseMapAlphaMin,
                        Mathf.Max(0.01f, _returnToBreathDuration)
                    )
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        _isDeactivating = false;
                        EnsureBreathing();
                    });
                if (CanToggleFruitObject()) _fruitObject.SetActive(false);
            });
        }
    }

    private Sequence AnimateParticles(Vector3 targetScale)
    {
        Sequence seq = DOTween.Sequence();
        if (_particleEffects == null || _particleEffects.Length == 0) return seq;

        for (int i = 0; i < _particleEffects.Length; i++)
        {
            Transform effect = _particleEffects[i];
            if (effect == null) continue;
            seq.Join(effect.DOScale(targetScale, _particleScaleDuration).SetEase(Ease.OutQuad));
        }

        return seq;
    }

    private void SetParticleScales(Vector3 scale)
    {
        if (_particleEffects == null || _particleEffects.Length == 0) return;

        for (int i = 0; i < _particleEffects.Length; i++)
        {
            Transform effect = _particleEffects[i];
            if (effect == null) continue;
            effect.localScale = scale;
        }
    }


    private void KillSequences()
    {
        _activateSequence?.Kill();
        _deactivateSequence?.Kill();
        _activateSequence = null;
        _deactivateSequence = null;
    }

    private void OnDisable()
    {
        KillSequences();
        StopBreathing();
        StopFlyLoopAudio();
    }

    private void StartFlyLoopAudio()
    {
        if (_flyLoopSource != null) return;
        MusicMgr.Instance?.PlaySound(AudioID.SFX_Platform_fly, true, source => _flyLoopSource = source);
    }

    private void StopFlyLoopAudio()
    {
        if (_flyLoopSource == null) return;
        MusicMgr.Instance?.StopSound(_flyLoopSource);
        _flyLoopSource = null;
    }

    private void ResolveFruitScales()
    {
        if (_useRelativeFruitScale)
        {
            _fruitScaleSmallResolved = Vector3.Scale(_fruitScaleNormal, _fruitScaleSmallMultiplier);
            _fruitScaleOvershootResolved = Vector3.Scale(_fruitScaleNormal, _fruitScaleOvershootMultiplier);
        }
        else
        {
            _fruitScaleSmallResolved = _fruitScaleSmall;
            _fruitScaleOvershootResolved = _fruitScaleOvershoot;
        }
    }

    private bool CanToggleFruitObject()
    {
        return _fruitObject != null && !_fruitIsSelf && !_fruitIsParent;
    }

    private void LogState(float alpha)
    {
        float rounded = Mathf.Round(alpha * 100f) * 0.01f;
        if (Mathf.Approximately(rounded, _lastLoggedAlpha)) return;
        _lastLoggedAlpha = rounded;
        Debug.Log($"[StandardBlock] alpha={rounded:F2}, activated={_activated}, breathe={_breatheTween != null}", this);
    }
}


