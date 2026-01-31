using UnityEngine;

public class MouseFollow : MonoBehaviour
{
    [Header("设置")]
    [SerializeField] private LayerMask _groundLayer; // 射线检测的层（如地面）
    [SerializeField] private float _followSpeed = 15f; // 跟随平滑速度
    [SerializeField] private bool _useSmoothing = true; // 是否开启平滑跟随

    private Camera _mainCamera;

    private void Awake()
    {
        _mainCamera = Camera.main;
    }

    private void Update()
    {
        MoveObjectToMouse();
    }

    private void MoveObjectToMouse()
    {
        // 1. 从摄像机发射射线
        Ray ray = _mainCamera.ScreenPointToRay(Input.mousePosition);

        // 2. 进行射线检测
        // 我们假设物体在一个水平面上移动，或者检测特定的地面层
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, _groundLayer))
        {
            Vector3 targetPosition = hit.point;

            // 如果你不希望物体陷入地面，可以根据物体的尺寸增加一个 y 轴偏移
            // targetPosition.y += transform.localScale.y / 2f;

            //z轴保持不变
            targetPosition.z = transform.position.z;
            if (_useSmoothing)
            {
                // 使用 Lerp 实现平滑跟随，手感更佳
                transform.position = Vector3.Lerp(transform.position, targetPosition, _followSpeed * Time.deltaTime);
            }
            else
            {
                // 瞬间跟随
                transform.position = targetPosition;
            }
        }
    }
}