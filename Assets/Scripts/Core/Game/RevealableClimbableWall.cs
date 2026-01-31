using UnityEngine;

public class RevealableClimbableWall : BaseRevealableBlock
{
    [Header("Layer Switch")]
    [SerializeField] private LayerMask _visibleLayer;
    [SerializeField] private LayerMask _hiddenLayer;

    private int _cachedLayer;
    private bool _warnedMultipleVisible;
    private bool _warnedMultipleHidden;

    protected override void Awake()
    {
        base.Awake();
        _cachedLayer = gameObject.layer;
    }

    protected override void Update()
    {
        base.Update();
        UpdateLayerByAlpha();
    }

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
            {
                gameObject.layer = visibleLayer;
            }
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

        if ((value & (value - 1)) != 0 && !warnedMultiple)
        {
            warnedMultiple = true;
            Debug.LogWarning("Only one layer is supported; using the first selected layer.", this);
        }

        int layer = 0;
        while ((value & 1) == 0)
        {
            value >>= 1;
            layer++;
        }

        return layer;
    }
}
