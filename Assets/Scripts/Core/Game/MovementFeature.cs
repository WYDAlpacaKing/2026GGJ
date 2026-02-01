using System.Collections.Generic;
using UnityEngine;
using Alpaca.Game.Audio;

public class MovementFeature : MonoBehaviour
{
    [Header("Path")]
    [SerializeField] private Transform _waypointRoot;
    [SerializeField] private bool _useChildWaypoints = true;
    [SerializeField] private List<Transform> _waypoints = new List<Transform>();
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private bool _loop = true;
    [SerializeField] private bool _pingPong;
    [SerializeField] private bool _lockZ = true;
    [SerializeField] private bool _debugLog;
    [SerializeField] private float _externalMoveThreshold = 0.001f;
    [SerializeField] private float _movingAudioMinDistance = 0.001f;

    private readonly List<Vector3> _points = new List<Vector3>();
    private int _currentIndex;
    private int _direction = 1;
    private float _lockedZ;
    private Vector3 _lastLoggedPosition;
    private AudioSource _movingLoopSource;
    private bool _wasMoving;

    private void Awake()
    {
        _lockedZ = transform.position.z;
        BuildPoints();
    }

    private void Update()
    {
        if (_points.Count == 0) return;

        Vector3 startPos = transform.position;
        Vector3 target = _points[_currentIndex];
        if (_lockZ) target.z = _lockedZ;
        Vector3 expected = Vector3.MoveTowards(
            startPos,
            target,
            _moveSpeed * Time.deltaTime
        );
        transform.position = expected;

        if (_lockZ)
        {
            Vector3 pos = transform.position;
            pos.z = _lockedZ;
            transform.position = pos;
            expected.z = _lockedZ;
        }

        if ((transform.position - target).sqrMagnitude <= 0.0001f)
        {
            transform.position = target;
            AdvanceIndex();
        }

        HandleMovingAudio();
        LogExternalMove(startPos, expected);
        LogState(target);
    }

    private void HandleMovingAudio()
    {
        bool isMoving = _points.Count > 0 && (transform.position - _points[_currentIndex]).sqrMagnitude >
                        _movingAudioMinDistance * _movingAudioMinDistance;

        if (isMoving && !_wasMoving)
        {
            StartMovingAudio();
        }
        else if (!isMoving && _wasMoving)
        {
            StopMovingAudio();
        }

        _wasMoving = isMoving;
    }

    private void StartMovingAudio()
    {
        if (_movingLoopSource != null) return;
        MusicMgr.Instance?.PlaySound(AudioID.SFX_Platform_moving, true, source => _movingLoopSource = source);
    }

    private void StopMovingAudio()
    {
        if (_movingLoopSource == null) return;
        MusicMgr.Instance?.StopSound(_movingLoopSource);
        _movingLoopSource = null;
    }

    private void OnDisable()
    {
        StopMovingAudio();
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
        SkipDuplicatePoints();
    }

    private void BuildPoints()
    {
        _points.Clear();
        AddPointIfDistinct(transform.position);

        if (_useChildWaypoints)
        {
            Transform root = _waypointRoot != null ? _waypointRoot : transform;
            for (int i = 0; i < root.childCount; i++)
            {
                AddPointIfDistinct(root.GetChild(i).position);
            }
        }
        else if (_waypoints != null)
        {
            foreach (Transform waypoint in _waypoints)
            {
                if (waypoint == null) continue;
                AddPointIfDistinct(waypoint.position);
            }
        }

        _currentIndex = 0;
        _direction = 1;
    }

    private void AddPointIfDistinct(Vector3 point)
    {
        if (_points.Count == 0)
        {
            _points.Add(point);
            return;
        }

        if ((_points[_points.Count - 1] - point).sqrMagnitude <= 0.0001f) return;
        _points.Add(point);
    }

    private void SkipDuplicatePoints()
    {
        if (_points.Count <= 1) return;

        int safety = 0;
        while ((_points[_currentIndex] - transform.position).sqrMagnitude <= 0.0001f && safety < _points.Count)
        {
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

            if (next == _currentIndex) break;
            _currentIndex = next;
            safety++;
        }
    }

    private void LogState(Vector3 target)
    {
        if (!_debugLog) return;
        float dist = Vector3.Distance(transform.position, target);
        if ((_lastLoggedPosition - transform.position).sqrMagnitude < 0.0001f) return;
        _lastLoggedPosition = transform.position;
        Debug.Log($"[MovementFeature] idx={_currentIndex} dir={_direction} pos={transform.position} target={target} dist={dist:F4}", this);
    }

    private void LogExternalMove(Vector3 startPos, Vector3 expected)
    {
        if (!_debugLog) return;
        Vector3 actual = transform.position;
        Vector3 diff = actual - expected;
        if (diff.sqrMagnitude <= _externalMoveThreshold * _externalMoveThreshold) return;
        Debug.LogWarning($"[MovementFeature] External move detected start={startPos} expected={expected} actual={actual} diff={diff}", this);
    }
}
