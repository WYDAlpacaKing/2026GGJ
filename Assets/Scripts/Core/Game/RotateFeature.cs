using UnityEngine;

public class RotateFeature : MonoBehaviour
{
    public enum RotationDirection
    {
        Clockwise,
        CounterClockwise
    }

    [Header("Rotation")]
    [SerializeField] private RotationDirection _direction = RotationDirection.Clockwise;
    [SerializeField] private float _speedDegreesPerSecond = 90f;

    private void Update()
    {
        float sign = _direction == RotationDirection.Clockwise ? -1f : 1f;
        float delta = _speedDegreesPerSecond * sign * Time.deltaTime;
        transform.Rotate(0f, 0f, delta, Space.Self);
    }
}
