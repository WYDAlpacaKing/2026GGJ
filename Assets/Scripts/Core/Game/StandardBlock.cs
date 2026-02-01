using UnityEngine;


public class StandardBlock : BaseRevealableBlock
{
    [Header("表现参数 (Emission/Particles)")]
    [SerializeField] private Renderer _emissionRenderer;
    [SerializeField] private string _emissionPropertyName = "_EmissionColor";
    [SerializeField] private Color _emissionColor = Color.white;
    [SerializeField] private float _emissionIntensityFrom = 0f;
    [SerializeField] private float _emissionIntensityTo = 2f;
    [SerializeField] private Transform[] _particleEffects = new Transform[4];
    [SerializeField] private Vector3 _particleScaleFrom = Vector3.one;
    [SerializeField] private Vector3 _particleScaleTo = Vector3.one * 1.5f;

    protected override void Awake()
    {
        _emissionPropertyID = Shader.PropertyToID(_emissionPropertyName);
        base.Awake();

        if (_emissionRenderer == null)
        {
            _emissionRenderer = _renderer;
        }
    }

    protected override void OnFullyRevealed()
    {
        base.OnFullyRevealed();
        // AudioSource.PlayClipAtPoint(clip, transform.position);
    }

    protected override void UpdateVisuals(float alpha)
    {
        base.UpdateVisuals(alpha);

        if (_emissionRenderer == null) return;

        _emissionRenderer.GetPropertyBlock(_propBlock);
        float emissionIntensity = Mathf.Lerp(_emissionIntensityFrom, _emissionIntensityTo, alpha);
        _propBlock.SetColor(_emissionPropertyID, _emissionColor * emissionIntensity);
        _emissionRenderer.SetPropertyBlock(_propBlock);

        UpdateParticleScales(alpha);
    }

    private void UpdateParticleScales(float alpha)
    {
        if (_particleEffects == null || _particleEffects.Length == 0) return;

        Vector3 scale = Vector3.Lerp(_particleScaleFrom, _particleScaleTo, alpha);
        for (int i = 0; i < _particleEffects.Length; i++)
        {
            Transform effect = _particleEffects[i];
            if (effect == null) continue;
            effect.localScale = scale;
        }
    }
}

