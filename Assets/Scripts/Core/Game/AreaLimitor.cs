using UnityEngine;

public class AreaLimitor : MonoBehaviour
{
    [Header("Bounds")]
    [SerializeField] private Transform _minPoint;
    [SerializeField] private Transform _maxPoint;

    private Transform _target;

    private void Awake()
    {
        _target = transform.parent != null ? transform.parent : transform;
    }

    private void LateUpdate()
    {
        if (_minPoint == null || _maxPoint == null || _target == null) return;

        Vector3 min = _minPoint.position;
        Vector3 max = _maxPoint.position;
        float minX = Mathf.Min(min.x, max.x);
        float maxX = Mathf.Max(min.x, max.x);
        float minY = Mathf.Min(min.y, max.y);
        float maxY = Mathf.Max(min.y, max.y);

        Vector3 pos = _target.position;
        pos.x = Mathf.Clamp(pos.x, minX, maxX);
        pos.y = Mathf.Clamp(pos.y, minY, maxY);
        _target.position = pos;
    }
}
