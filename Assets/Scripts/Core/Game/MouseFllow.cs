using UnityEngine;

public class MouseFollow : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private MouseFollowConfig _config;

    [Header("Reveal Control")]
    [SerializeField] private bool _canReveal = true;

    private Camera _mainCamera;

    private void Awake()
    {
        _mainCamera = Camera.main;
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

            targetPosition.z = transform.position.z;
            if (_config.MouseUseSmoothing)
            {
                // ??? Lerp ?????????��??��???
                transform.position = Vector3.Lerp(
                    transform.position,
                    targetPosition,
                    _config.MouseLerpSpeed * Time.deltaTime
                );
            }
            else
            {
                // ??????
                transform.position = targetPosition;
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

        Vector3 targetPosition = transform.position + input * (_config.KeyboardMoveSpeed * Time.deltaTime);
        targetPosition.z = transform.position.z;

        if (_config.KeyboardUseSmoothing)
        {
            transform.position = Vector3.Lerp(
                transform.position,
                targetPosition,
                _config.KeyboardLerpSpeed * Time.deltaTime
            );
        }
        else
        {
            transform.position = targetPosition;
        }
    }
}