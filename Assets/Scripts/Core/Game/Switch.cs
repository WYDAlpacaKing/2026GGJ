using UnityEngine;

public class Switch : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform _targetTransform;
    [SerializeField] private Transform _targetPosition;

    [Header("Plate")]
    [SerializeField] private Transform _plateTransform;
    [SerializeField] private Vector3 _pressedLocalOffset = new Vector3(0f, -0.1f, 0f);

    [Header("Timing")]
    [SerializeField] private float _pressDuration = 0.5f;
    [SerializeField] private float _returnDuration = 1.5f;

    private Vector3 _initialTargetPosition;
    private Vector3 _plateUpLocalPosition;
    private Vector3 _plateDownLocalPosition;
    private Vector3 _pressStartLocalPosition;
    private Vector3 _returnStartLocalPosition;
    private bool _playerInside;
    private float _stateTime;

    private enum SwitchState
    {
        Idle,
        Pressing,
        Active,
        Returning
    }

    private SwitchState _state = SwitchState.Idle;

    private void Awake()
    {
        if (_targetTransform != null)
        {
            _initialTargetPosition = _targetTransform.position;
        }

        if (_plateTransform == null)
        {
            _plateTransform = transform;
        }

        _plateUpLocalPosition = _plateTransform.localPosition;
        _plateDownLocalPosition = _plateUpLocalPosition + _pressedLocalOffset;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        _playerInside = true;

        if (_state == SwitchState.Idle || _state == SwitchState.Returning)
        {
            StartPressing();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        _playerInside = false;

        if (_state == SwitchState.Pressing || _state == SwitchState.Active)
        {
            StartReturning();
        }
    }

    private void Update()
    {
        switch (_state)
        {
            case SwitchState.Idle:
                SetPlateLocalPosition(_plateUpLocalPosition);
                break;
            case SwitchState.Pressing:
                UpdatePressing();
                break;
            case SwitchState.Active:
                SetPlateLocalPosition(_plateDownLocalPosition);
                if (!_playerInside) StartReturning();
                break;
            case SwitchState.Returning:
                UpdateReturning();
                break;
        }
    }

    private void UpdatePressing()
    {
        if (!_playerInside)
        {
            StartReturning();
            return;
        }

        float t = GetNormalizedTime(_pressDuration);
        SetPlateLocalPosition(Vector3.Lerp(_pressStartLocalPosition, _plateDownLocalPosition, t));

        if (t >= 1f)
        {
            Activate();
        }
    }

    private void UpdateReturning()
    {
        float t = GetNormalizedTime(_returnDuration);
        SetPlateLocalPosition(Vector3.Lerp(_returnStartLocalPosition, _plateUpLocalPosition, t));

        if (t >= 1f)
        {
            Deactivate();
        }
    }

    private void StartPressing()
    {
        _state = SwitchState.Pressing;
        _stateTime = 0f;
        _pressStartLocalPosition = _plateTransform.localPosition;
    }

    private void StartReturning()
    {
        _state = SwitchState.Returning;
        _stateTime = 0f;
        _returnStartLocalPosition = _plateTransform.localPosition;
    }

    private void Activate()
    {
        _state = SwitchState.Active;

        if (_targetTransform != null && _targetPosition != null)
        {
            _targetTransform.position = _targetPosition.position;
        }
    }

    private void Deactivate()
    {
        _state = SwitchState.Idle;

        if (_targetTransform != null)
        {
            _targetTransform.position = _initialTargetPosition;
        }
    }

    private float GetNormalizedTime(float duration)
    {
        if (duration <= 0f) return 1f;
        _stateTime += Time.deltaTime;
        return Mathf.Clamp01(_stateTime / duration);
    }

    private void SetPlateLocalPosition(Vector3 position)
    {
        _plateTransform.localPosition = position;
    }
}
