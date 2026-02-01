using UnityEngine;

[RequireComponent(typeof(Collider))] // Trigger Collider
[RequireComponent(typeof(Renderer))]
public class BaseRevealableBlock : MonoBehaviour
{
    [Header("ʾ������")]
    [SerializeField] protected float _baseRevealDuration = 2.0f; 
    [SerializeField] protected float _vanishDuration = 1.0f;     
    [SerializeField] protected Collider _solidCollider;          

    [Header("�Ӿ�����")]
    [SerializeField] protected Color _hiddenColor = new Color(1, 1, 1, 0);
    [SerializeField] protected Color _visibleColor = Color.white;
    [SerializeField] protected string _colorPropertyName = "_BaseColor";

    // �ڲ�����
    protected float _currentAlpha = 0f;
    protected bool _isBrushInside = false;
    protected Transform _brushTransform;

    // ��Ⱦ�Ż�
    protected Renderer _renderer;
    protected MaterialPropertyBlock _propBlock;
    protected int _colorPropertyID;
    protected int _emissionPropertyID;

    protected virtual void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _propBlock = new MaterialPropertyBlock();
        _colorPropertyID = Shader.PropertyToID(_colorPropertyName); // ����ID��������
        _emissionPropertyID = Shader.PropertyToID("_EmissionColor");

        // ȷ����Collider��Trigger
        GetComponent<Collider>().isTrigger = true;

        // ��ʼ״̬������ײ��ȫ͸��
        if (_solidCollider != null) _solidCollider.enabled = false;
        UpdateVisuals(0f);
    }

    protected virtual void Update()
    {
        HandleStateLogic();
    }

    private void HandleStateLogic()
    {
        // �����߼��ع��������������ӵ� switch ״̬�������ǻ��ڵ�ǰ�������㡰Ŀ����Ϊ��

        // �������������ڴ������ڣ�����Ұ�ס���
        //bool isInteracting = _isBrushInside && _brushTransform != null && Input.GetMouseButton(0);
        bool isInteracting = _isBrushInside && _brushTransform != null && IsBrushAllowed(_brushTransform);

        if (isInteracting)
        {
            // --- �����߼� ---
            float overlapFactor = CalculateOverlapFactor();
            // �غ϶�Խ�ߣ��ٶ�Խ�� (1�� ~ 4����)
            float speed = (1f / _baseRevealDuration) * (1f + overlapFactor * 3f);

            _currentAlpha = Mathf.MoveTowards(_currentAlpha, 1f, speed * Time.deltaTime);
        }
        else
        {
            // --- ��ʧ�߼� ---
            // ֻҪ�����㽻�����������Զ�������ʧ
            float speed = 1f / _vanishDuration;
            _currentAlpha = Mathf.MoveTowards(_currentAlpha, 0f, speed * Time.deltaTime);
        }

        // --- ��ײ��״̬���� ---
        UpdateSolidCollider();

        // --- ���»��� ---
        UpdateVisuals(_currentAlpha);
    }

    protected virtual void OnFullyRevealed()
    {

    }

    protected virtual void UpdateSolidCollider()
    {
        // ֻ�е���ȫ����(Alpha >= 1)ʱ��������ײ
        // ֻ�е���ȫ��ʧ(Alpha <= 0)ʱ���ر���ײ (�������������ʧʱ���������ײ����ʧ)
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
    }

    protected float CalculateOverlapFactor()
    {
        if (_brushTransform == null) return 0;
        // �򵥵ľ���˥���㷨
        float maxDist = transform.localScale.x * 0.8f;
        float dist = Vector3.Distance(transform.position, _brushTransform.position);
        float factor = 1f - Mathf.Clamp01(dist / maxDist);
        return factor * factor;
    }

    protected virtual void UpdateVisuals(float alpha)
    {
        // ��ɫ��ֵ
        Color c = Color.Lerp(_hiddenColor, _visibleColor, alpha);

        _renderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor(_colorPropertyID, c); // ʹ�û����ID
        _renderer.SetPropertyBlock(_propBlock);
    }

    // --- �������߼����ּ� ---
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Brush"))
        {
            if (IsBrushAllowed(other.transform))
            {
                _isBrushInside = true;
                _brushTransform = other.transform;
            }
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

    private bool IsBrushAllowed(Transform brushTransform)
    {
        if (brushTransform == null) return false;
        if (!brushTransform.TryGetComponent(out MouseFollow mouseFollow)) return true;
        return mouseFollow.CanReveal;
    }
}