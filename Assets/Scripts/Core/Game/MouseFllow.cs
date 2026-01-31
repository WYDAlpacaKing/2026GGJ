using UnityEngine;

public class MouseFollow : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private MouseFollowConfig _config;

    [Header("Reveal Control")]
    [SerializeField] private bool _canReveal = true;

    [Header("Follow Target")]
    [SerializeField] private Transform _followRoot;

    [Header("Collider Facing")]
    [SerializeField] private Transform _colliderTransform;
    [SerializeField] private Vector3 _upDirection = Vector3.up;

    private Camera _mainCamera;

    private void Awake()
    {
        _mainCamera = Camera.main;

        if (_followRoot == null)
        {
            _followRoot = transform.parent != null ? transform.parent : transform;
        }
    }

    private void Update()
    {
        if (_config == null)
        {
            return;
        }

        switch (_config.MoveModeValue)
        {
            case MouseFollowConfig.MoveMode.MouseFollow:
                MoveObjectToMouse();
                break;
            case MouseFollowConfig.MoveMode.Keyboard:
                MoveObjectByKeyboard();
                break;
        }

        UpdateColliderFacing();
    }

    public bool CanReveal => _canReveal;

    public void SetCanReveal(bool canReveal)
    {
        _canReveal = canReveal;
    }

    private void MoveObjectToMouse()
    {
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _config.GroundLayer))
        {
            Vector3 targetPosition = hit.point;

            targetPosition.z = _followRoot.position.z;
            if (_config.MouseUseSmoothing)
            {
                // ??? Lerp ?????????��??��???
                _followRoot.position = Vector3.Lerp(
                    _followRoot.position,
                    targetPosition,
                    _config.MouseLerpSpeed * Time.deltaTime
                );
            }
            else
            {
                // ??????
                _followRoot.position = targetPosition;
            }
        }
    }

    private void MoveObjectByKeyboard()
    {
        Vector3 input = new Vector3(Input.GetAxisRaw("Horizontal2"), Input.GetAxisRaw("Vertical2"), 0f);
        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        Vector3 targetPosition = _followRoot.position + input * (_config.KeyboardMoveSpeed * Time.deltaTime);
        targetPosition.z = _followRoot.position.z;

        if (_config.KeyboardUseSmoothing)
        {
            _followRoot.position = Vector3.Lerp(
                _followRoot.position,
                targetPosition,
                _config.KeyboardLerpSpeed * Time.deltaTime
            );
        }
        else
        {
            _followRoot.position = targetPosition;
        }
    }

    private void UpdateColliderFacing()
    {
        if (_mainCamera == null) return;

        Transform target = _colliderTransform != null ? _colliderTransform : transform;
        Vector3 toTarget = target.position - _mainCamera.transform.position;
        if (toTarget.sqrMagnitude <= 0.0001f) return;

        Quaternion lookRotation = Quaternion.LookRotation(toTarget.normalized, _upDirection);
        target.rotation = lookRotation;
    }
}