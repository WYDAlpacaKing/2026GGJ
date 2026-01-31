using System.Collections.Generic;
using UnityEngine;

public class MovementFeature : MonoBehaviour
{
    [Header("Path")]
    [SerializeField] private Transform _waypointRoot;
    [SerializeField] private bool _useChildWaypoints = true;
    [SerializeField] private List<Transform> _waypoints = new List<Transform>();
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private bool _loop = true;
    [SerializeField] private bool _pingPong;

    private readonly List<Vector3> _points = new List<Vector3>();
    private int _currentIndex;
    private int _direction = 1;

    private void Awake()
    {
        BuildPoints();
    }

    private void Update()
    {
        if (_points.Count == 0) return;

        Vector3 target = _points[_currentIndex];
        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            _moveSpeed * Time.deltaTime
        );

        if ((transform.position - target).sqrMagnitude <= 0.0001f)
        {
            AdvanceIndex();
        }
    }

    private void AdvanceIndex()
    {
        if (_points.Count <= 1) return;

        int next = _currentIndex + _direction;

        if (_pingPong)
        {
            if (next >= _points.Count || next < 0)
            {
                _direction *= -1;
                next = _currentIndex + _direction;
            }
        }
        else if (_loop)
        {
            if (next >= _points.Count) next = 0;
            if (next < 0) next = _points.Count - 1;
        }
        else
        {
            next = Mathf.Clamp(next, 0, _points.Count - 1);
        }

        _currentIndex = next;
    }

    private void BuildPoints()
    {
        _points.Clear();
        _points.Add(transform.position);

        if (_useChildWaypoints)
        {
            Transform root = _waypointRoot != null ? _waypointRoot : transform;
            for (int i = 0; i < root.childCount; i++)
            {
                _points.Add(root.GetChild(i).position);
            }
        }
        else if (_waypoints != null)
        {
            foreach (Transform waypoint in _waypoints)
            {
                if (waypoint == null) continue;
                _points.Add(waypoint.position);
            }
        }

        _currentIndex = 0;
        _direction = 1;
    }
}
