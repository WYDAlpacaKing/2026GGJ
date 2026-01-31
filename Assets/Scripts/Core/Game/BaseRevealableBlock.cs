using UnityEngine;

[RequireComponent(typeof(Collider))] // Trigger Collider
[RequireComponent(typeof(Renderer))]
public class BaseRevealableBlock : MonoBehaviour
{
    [Header("显形设置")]
    [SerializeField] protected float _baseRevealDuration = 2.0f; // 显形基准时间
    [SerializeField] protected float _vanishDuration = 1.0f;     // 消失时间
    [SerializeField] protected Collider _solidCollider;          // 物理碰撞体

    [Header("视觉反馈")]
    [SerializeField] protected Color _hiddenColor = new Color(1, 1, 1, 0);
    [SerializeField] protected Color _visibleColor = Color.white;
    [SerializeField] protected string _colorPropertyName = "_BaseColor";

    // 内部变量
    protected float _currentAlpha = 0f;
    protected bool _isBrushInside = false;
    protected Transform _brushTransform;

    // 渲染优化
    protected Renderer _renderer;
    protected MaterialPropertyBlock _propBlock;
    protected int _colorPropertyID;

    protected virtual void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _propBlock = new MaterialPropertyBlock();
        _colorPropertyID = Shader.PropertyToID(_colorPropertyName); // 缓存ID提升性能

        // 确保主Collider是Trigger
        GetComponent<Collider>().isTrigger = true;

        // 初始状态：无碰撞，全透明
        if (_solidCollider != null) _solidCollider.enabled = false;
        UpdateVisuals(0f);
    }

    protected virtual void Update()
    {
        HandleStateLogic();
    }

    private void HandleStateLogic()
    {
        // 核心逻辑重构：不再依赖复杂的 switch 状态机，而是基于当前条件计算“目标行为”

        // 条件：鼠标必须在触发器内，且玩家按住左键
        bool isInteracting = _isBrushInside && _brushTransform != null && Input.GetMouseButton(0);

        if (isInteracting)
        {
            // --- 显形逻辑 ---
            float overlapFactor = CalculateOverlapFactor();
            // 重合度越高，速度越快 (1倍 ~ 4倍速)
            float speed = (1f / _baseRevealDuration) * (1f + overlapFactor * 3f);

            _currentAlpha = Mathf.MoveTowards(_currentAlpha, 1f, speed * Time.deltaTime);
        }
        else
        {
            // --- 消失逻辑 ---
            // 只要不满足交互条件，就自动回退消失
            float speed = 1f / _vanishDuration;
            _currentAlpha = Mathf.MoveTowards(_currentAlpha, 0f, speed * Time.deltaTime);
        }

        // --- 碰撞体状态管理 ---
        // 只有当完全显形(Alpha >= 1)时，开启碰撞
        // 只有当完全消失(Alpha <= 0)时，关闭碰撞 (根据你的需求，消失时间结束后碰撞才消失)

        if (_currentAlpha >= 0.99f)
        {
            if (_solidCollider != null && !_solidCollider.enabled)
                _solidCollider.enabled = true;
            OnFullyRevealed();
        }
        else if (_currentAlpha <= 0.01f)
        {
            if (_solidCollider != null && _solidCollider.enabled)
                _solidCollider.enabled = false;
        }

        // --- 更新画面 ---
        UpdateVisuals(_currentAlpha);
    }

    protected virtual void OnFullyRevealed()
    {

    }

    protected float CalculateOverlapFactor()
    {
        if (_brushTransform == null) return 0;
        // 简单的距离衰减算法
        float maxDist = transform.localScale.x * 0.8f;
        float dist = Vector3.Distance(transform.position, _brushTransform.position);
        float factor = 1f - Mathf.Clamp01(dist / maxDist);
        return factor * factor;
    }

    protected void UpdateVisuals(float alpha)
    {
        // 颜色插值
        Color c = Color.Lerp(_hiddenColor, _visibleColor, alpha);

        _renderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor(_colorPropertyID, c); // 使用缓存的ID
        _renderer.SetPropertyBlock(_propBlock);
    }

    // --- 触发器逻辑保持简单 ---
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Brush"))
        {
            _isBrushInside = true;
            _brushTransform = other.transform;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Brush"))
        {
            _isBrushInside = false;
            _brushTransform = null;
        }
    }
}