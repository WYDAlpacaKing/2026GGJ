using UnityEngine;

public class RevealableClimbableWall : BaseRevealableBlock
{
    [Header("Layer Switch")]
    [SerializeField] private LayerMask _visibleLayer;
    [SerializeField] private LayerMask _hiddenLayer;

    [Header("Child Renderer Settings")]
    [Tooltip("持有材质的子物体。若为空，则自动查找第一个子物体渲染器。")]
    [SerializeField] private Renderer _childRenderer;

    [Header("Emission Settings (Overrides Parent)")]
    [Tooltip("指定发光的颜色 (HDR 基准色)")]
    [SerializeField] private Color _emissionColor = Color.white;
    [SerializeField] private float _minEmissionIntensity = 0f;
    [SerializeField] private float _maxEmissionIntensity = 4.0f;

    private int _cachedLayer;
    private bool _warnedMultipleVisible;
    private bool _warnedMultipleHidden;

    private MaterialPropertyBlock _childPropBlock;

    protected override void Awake()
    {
        // 1. 初始化子物体渲染器引用
        if (_childRenderer == null)
        {
            _childRenderer = GetComponentInChildren<Renderer>();
        }

        _childPropBlock = new MaterialPropertyBlock();

        // 2. 调用父类初始化（保留 Collider 和逻辑初始化）
        base.Awake();

        _cachedLayer = gameObject.layer;
        if (_solidCollider != null) _solidCollider.isTrigger = true;
    }

    protected override void Update()
    {
        base.Update();
        UpdateLayerByAlpha();
    }

    /// <summary>
    /// 完全重写视觉逻辑：指定颜色并控制强度
    /// 不调用 base.UpdateVisuals(alpha)，因此父类的 _BaseColor 逻辑被彻底屏蔽
    /// </summary>
    protected override void UpdateVisuals(float alpha)
    {
        if (_childRenderer == null) return;

        // 计算当前线性强度
        float currentIntensity = Mathf.Lerp(_minEmissionIntensity, _maxEmissionIntensity, alpha);

        // 使用指定的 _emissionColor 乘以强度得到 HDR 最终发光色
        Color finalEmissionColor = _emissionColor * currentIntensity;

        // 应用到独立的 PropertyBlock
        _childRenderer.GetPropertyBlock(_childPropBlock);

        // 设置 _EmissionColor 属性
        _childPropBlock.SetColor("_EmissionColor", finalEmissionColor);

        _childRenderer.SetPropertyBlock(_childPropBlock);
    }

    // --- 保持物理与层级逻辑 ---

    protected override void UpdateSolidCollider()
    {
        if (_solidCollider != null && !_solidCollider.enabled)
        {
            _solidCollider.enabled = true;
        }

        if (_currentAlpha >= 0.99f)
        {
            OnFullyRevealed();
            _solidCollider.isTrigger = false;
        }
    }

    private void UpdateLayerByAlpha()
    {
        int visibleLayer = GetLayerIndex(_visibleLayer, -1, ref _warnedMultipleVisible);
        int hiddenLayer = GetLayerIndex(_hiddenLayer, _cachedLayer, ref _warnedMultipleHidden);

        if (_currentAlpha >= 0.99f)
        {
            if (visibleLayer >= 0 && gameObject.layer != visibleLayer)
                gameObject.layer = visibleLayer;
        }
        else if (_currentAlpha <= 0.01f)
        {
            if (hiddenLayer >= 0 && gameObject.layer != hiddenLayer)
            {
                gameObject.layer = hiddenLayer;
                _solidCollider.isTrigger = true;
            }
        }
    }

    private int GetLayerIndex(LayerMask mask, int fallbackLayer, ref bool warnedMultiple)
    {
        int value = mask.value;
        if (value == 0) return fallbackLayer;
        int layer = 0;
        while ((value & 1) == 0) { value >>= 1; layer++; }
        return layer;
    }
}