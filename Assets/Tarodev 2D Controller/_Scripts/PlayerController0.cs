using System;
using UnityEngine;

namespace TarodevController.old
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class PlayerController0 : MonoBehaviour, IPlayerController
    {
        [SerializeField] private ScriptableStats _stats;
        private Rigidbody _rb;
        private CapsuleCollider _col;
        private FrameInput _frameInput;
        private Vector3 _frameVelocity;
        private bool _cachedQueryStartInColliders;

        #region Interface
        public Vector2 FrameInput => _frameInput.Move;
        public event Action<bool, float> GroundedChanged;
        public event Action Jumped;
        #endregion

        private float _time;
        private bool _isWallSliding;
        private Vector3 _wallHitNormal; // [新增] 存储墙壁法线

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _col = GetComponent<CapsuleCollider>();

            _rb.constraints = RigidbodyConstraints.FreezeRotation;
            _cachedQueryStartInColliders = Physics.queriesHitTriggers;
        }

        private void Update()
        {
            _time += Time.deltaTime;
            GatherInput();
        }

        private void GatherInput()
        {
            _frameInput = new FrameInput
            {
                JumpDown = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.C),
                JumpHeld = Input.GetButton("Jump") || Input.GetKey(KeyCode.C),
                Move = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"))
            };

            if (_stats.SnapInput)
            {
                _frameInput.Move.x = Mathf.Abs(_frameInput.Move.x) < _stats.HorizontalDeadZoneThreshold ? 0 : Mathf.Sign(_frameInput.Move.x);
                _frameInput.Move.y = Mathf.Abs(_frameInput.Move.y) < _stats.VerticalDeadZoneThreshold ? 0 : Mathf.Sign(_frameInput.Move.y);
            }

            if (_frameInput.JumpDown)
            {
                _jumpToConsume = true;
                _timeJumpWasPressed = _time;
            }
        }

        private void FixedUpdate()
        {
            CheckCollisions();
            CheckWallSlide();

            HandleJump();
            HandleDirection();
            HandleGravity();

            ApplyMovement();
        }

        #region Collisions

        private float _frameLeftGrounded = float.MinValue;
        private bool _grounded;

        private void CheckCollisions()
        {
            Physics.queriesHitTriggers = false;

            Vector3 center = transform.position + _col.center;
            float castDistance = _stats.GrounderDistance;

            bool groundHit = Physics.SphereCast(center, _col.radius, Vector3.down, out _, _col.height / 2f - _col.radius + castDistance, ~_stats.PlayerLayer);
            bool ceilingHit = Physics.SphereCast(center, _col.radius, Vector3.up, out _, _col.height / 2f - _col.radius + castDistance, ~_stats.PlayerLayer);

            if (ceilingHit) _frameVelocity.y = Mathf.Min(0, _frameVelocity.y);

            if (!_grounded && groundHit)
            {
                _grounded = true;
                _coyoteUsable = true;
                _bufferedJumpUsable = true;
                _endedJumpEarly = false;
                GroundedChanged?.Invoke(true, Mathf.Abs(_frameVelocity.y));
            }
            else if (_grounded && !groundHit)
            {
                _grounded = false;
                _frameLeftGrounded = _time;
                GroundedChanged?.Invoke(false, 0);
            }

            Physics.queriesHitTriggers = _cachedQueryStartInColliders;
        }

        #endregion

        #region Wall Slide

        private void CheckWallSlide()
        {
            _isWallSliding = false;

            if (_grounded || _frameVelocity.y > 0) return;
            if (_frameInput.Move.sqrMagnitude < 0.01f) return;

            Vector3 inputDir = new Vector3(_frameInput.Move.x, 0, _frameInput.Move.y).normalized;
            Vector3 colCenter = transform.position + _col.center;
            float halfHeight = _col.height / 2f;
            float shrinkAmount = 0.1f;
            Vector3 point1 = colCenter + Vector3.up * (halfHeight - _col.radius - shrinkAmount);
            Vector3 point2 = colCenter - Vector3.up * (halfHeight - _col.radius - shrinkAmount);

            if (Physics.CapsuleCast(point1, point2, _col.radius - 0.05f, inputDir, out RaycastHit hit, _stats.WallDetectionDistance, _stats.ClimbableLayer))
            {
                _isWallSliding = true;
                _wallHitNormal = hit.normal; // [新增] 关键：记录墙壁的法线方向
            }
        }


        #endregion

        #region Jumping

        private bool _jumpToConsume;
        private bool _bufferedJumpUsable;
        private bool _endedJumpEarly;
        private bool _coyoteUsable;
        private float _timeJumpWasPressed;

        private bool HasBufferedJump => _bufferedJumpUsable && _time < _timeJumpWasPressed + _stats.JumpBuffer;
        private bool CanUseCoyote => _coyoteUsable && !_grounded && _time < _frameLeftGrounded + _stats.CoyoteTime;

        private void HandleJump()
        {
            if (!_endedJumpEarly && !_grounded && !_frameInput.JumpHeld && _rb.linearVelocity.y > 0) _endedJumpEarly = true;

            if (!_jumpToConsume && !HasBufferedJump) return;

            // [新增] 蹬墙跳优先级高于普通跳跃
            if (_isWallSliding)
            {
                ExecuteWallJump();
                _jumpToConsume = false; // 消耗掉跳跃输入
                return; // 跳出，不再执行下面的普通跳跃逻辑
            }

            if (_grounded || CanUseCoyote) ExecuteJump();

            _jumpToConsume = false;
        }

        private void ExecuteJump() // 普通地面跳跃
        {
            _endedJumpEarly = false;
            _timeJumpWasPressed = 0;
            _bufferedJumpUsable = false;
            _coyoteUsable = false;
            _frameVelocity.y = _stats.JumpPower;
            Jumped?.Invoke();
        }



        private void ExecuteWallJump() // [新增] 蹬墙跳
        {
            _endedJumpEarly = false;
            _bufferedJumpUsable = false;
            _timeJumpWasPressed = 0;

            // 1. 核心逻辑：沿法线弹开 + 向上弹起
            // 使用法线(Normal)乘以水平力度，加上Vector3.up乘以垂直力度
            Vector3 jumpDir = _wallHitNormal * _stats.WallJumpHorizontalPower;
            jumpDir.y = _stats.WallJumpVerticalPower;

            _frameVelocity = jumpDir;

            // 2. 关键修正：立刻退出滑墙状态
            // 如果不加这行，HandleDirection会在同一帧内检测到 isWallSliding 为真，
            // 从而把我们刚赋值的 X/Z 速度强制归零。
            _isWallSliding = false;

            Jumped?.Invoke();
        }

        #endregion

        #region Horizontal

        private void HandleDirection()
        {
            if (_isWallSliding)
            {
                _frameVelocity.x = 0;
                _frameVelocity.z = 0;
                return;
            }

            // [优化提示]：
            // 在蹬墙跳后，玩家通常会立刻按回墙的方向键。
            // 这里的 HandleDirection 会立刻产生一个反向加速度去抵消蹬墙跳的水平速度。
            // 如果觉得蹬墙跳“跳不远”，是因为这里的 AirAcceleration 太高了，或者需要添加短暂的“空中输入锁定(Air Lock)”。
            // 但为了保持代码简洁，暂时维持原样。

            if (_frameInput.Move.x == 0)
            {
                var deceleration = _grounded ? _stats.GroundDeceleration : _stats.AirDeceleration;
                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, 0, deceleration * Time.fixedDeltaTime);
            }
            else
            {
                _frameVelocity.x = Mathf.MoveTowards(_frameVelocity.x, _frameInput.Move.x * _stats.MaxSpeed, _stats.Acceleration * Time.fixedDeltaTime);
            }

            if (_frameInput.Move.y == 0)
            {
                var deceleration = _grounded ? _stats.GroundDeceleration : _stats.AirDeceleration;
                _frameVelocity.z = Mathf.MoveTowards(_frameVelocity.z, 0, deceleration * Time.fixedDeltaTime);
            }
            else
            {
                _frameVelocity.z = Mathf.MoveTowards(_frameVelocity.z, _frameInput.Move.y * _stats.MaxSpeed, _stats.Acceleration * Time.fixedDeltaTime);
            }
        }

        #endregion

        #region Gravity

        private void HandleGravity()
        {
            if (_isWallSliding)
            {
                _frameVelocity.y = Mathf.MoveTowards(_frameVelocity.y, -_stats.WallSlideSpeed, _stats.FallAcceleration * Time.fixedDeltaTime);
            }
            else if (_grounded && _frameVelocity.y <= 0f)
            {
                _frameVelocity.y = _stats.GroundingForce;
            }
            else
            {
                var inAirGravity = _stats.FallAcceleration;
                if (_endedJumpEarly && _frameVelocity.y > 0) inAirGravity *= _stats.JumpEndEarlyGravityModifier;
                _frameVelocity.y = Mathf.MoveTowards(_frameVelocity.y, -_stats.MaxFallSpeed, inAirGravity * Time.fixedDeltaTime);
            }
        }

        #endregion

        private void ApplyMovement() => _rb.linearVelocity = _frameVelocity;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_stats == null) Debug.LogWarning("Please assign a ScriptableStats asset", this);
        }
#endif

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (_col == null || _stats == null) return;
            // (保持之前的调试代码不变)
            Vector3 inputDir = Vector3.zero;
            if (_frameInput.Move.sqrMagnitude > 0.01f)
            {
                inputDir = new Vector3(_frameInput.Move.x, 0, _frameInput.Move.y).normalized;
            }
            if (inputDir == Vector3.zero) inputDir = transform.forward;
            Vector3 colCenter = transform.position + _col.center;
            float halfHeight = _col.height / 2f;
            Vector3 point1 = colCenter + Vector3.up * (halfHeight - _col.radius - 0.1f);
            Vector3 point2 = colCenter - Vector3.up * (halfHeight - _col.radius - 0.1f);

            bool isHit = Physics.CapsuleCast(point1, point2, _col.radius, inputDir, out RaycastHit hit, _stats.WallDetectionDistance, _stats.ClimbableLayer);

            Gizmos.color = isHit ? Color.green : Color.red;
            Gizmos.DrawWireSphere(point1, _col.radius);
            Gizmos.DrawWireSphere(point2, _col.radius);
            Vector3 endPoint = colCenter + inputDir * _stats.WallDetectionDistance;
            Gizmos.DrawLine(colCenter, endPoint);
            if (isHit)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawSphere(hit.point, 0.1f);
                // [新增调试] 画出法线方向，方便看蹬墙跳的方向
                Gizmos.color = Color.blue;
                Gizmos.DrawRay(hit.point, hit.normal);
            }
        }
#endif
    }

    public struct FrameInput
    {
        public bool JumpDown;
        public bool JumpHeld;
        public Vector2 Move;
    }

    public interface IPlayerController
    {
        public event Action<bool, float> GroundedChanged;
        public event Action Jumped;
        public Vector2 FrameInput { get; }
    }
}