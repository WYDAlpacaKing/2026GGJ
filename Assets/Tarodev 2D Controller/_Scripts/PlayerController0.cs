using System;
using UnityEngine;

namespace TarodevController.old
{
    [RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
    public class PlayerController0 : MonoBehaviour, IPlayerController
    {
        [SerializeField] private ScriptableStats _stats;
       
        public enum VisualRotateDirection
        {
            Clockwise,
            CounterClockwise
        }
        [SerializeField] private Transform _visual;
        [SerializeField] private float _visualRotateSpeed = 720f;
        [SerializeField] private VisualRotateDirection _visualRotateDirection = VisualRotateDirection.Clockwise;
        private Rigidbody _rb;
        private CapsuleCollider _col;
        private FrameInput _frameInput;
        private Vector3 _frameVelocity;
        private bool _cachedQueryStartInColliders;
        private float _forceUngroundTime;
        private float _springAssistTime;
        private float _springMaxUpSpeed;
        private float _springUpAcceleration;
        private Vector3 _springDirection = Vector3.up;
        private float _lockedZ;

        #region Interface
        public Vector2 FrameInput => _frameInput.Move;
        public event Action<bool, float> GroundedChanged;
        public event Action Jumped;
        #endregion

        private float _time;
        private bool _isWallSliding;
        private Vector3 _wallHitNormal; // [����] �洢ǽ�ڷ���

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _col = GetComponent<CapsuleCollider>();

            _rb.constraints = RigidbodyConstraints.FreezeRotation;
            _cachedQueryStartInColliders = Physics.queriesHitTriggers;
            _lockedZ = transform.position.z;

            if (_visual == null)
            {
                Transform visualChild = transform.Find("Visual");
                if (visualChild != null) _visual = visualChild;
            }
        }

        private void Update()
        {
            _time += Time.deltaTime;
            GatherInput();
            UpdateVisualFacing();
        }

        private void GatherInput()
        {
            _frameInput = new FrameInput
            {
                JumpDown = Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.C),
                JumpHeld = Input.GetButton("Jump") || Input.GetKey(KeyCode.C),
                Move = new Vector2(Input.GetAxisRaw("Horizontal"), 0f)
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

        private void UpdateVisualFacing()
        {
            if (_visual == null) return;
            if (Mathf.Abs(_frameInput.Move.x) < 0.01f) return;

            float targetYaw = _frameInput.Move.x > 0 ? 0f : 180f;
            float currentYaw = _visual.localEulerAngles.y;
            float delta = Mathf.DeltaAngle(currentYaw, targetYaw);
            if (_visualRotateDirection == VisualRotateDirection.Clockwise && delta > 0f)
            {
                delta -= 360f;
            }
            else if (_visualRotateDirection == VisualRotateDirection.CounterClockwise && delta < 0f)
            {
                delta += 360f;
            }

            float maxStep = _visualRotateSpeed * Time.deltaTime;
            float step = Mathf.Clamp(delta, -maxStep, maxStep);
            _visual.localRotation = Quaternion.Euler(0f, currentYaw + step, 0f);
        }

        private void FixedUpdate()
        {
            LockZAxis();
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

            if (_time < _forceUngroundTime)
            {
                groundHit = false;
            }

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

            Vector3 colCenter = transform.position + _col.center;
            float halfHeight = _col.height / 2f;
            float shrinkAmount = 0.1f;
            Vector3 point1 = colCenter + Vector3.up * (halfHeight - _col.radius - shrinkAmount);
            Vector3 point2 = colCenter - Vector3.up * (halfHeight - _col.radius - shrinkAmount);

            float checkRadius = _col.radius - 0.05f;
            bool hitRight = Physics.CapsuleCast(point1, point2, checkRadius, Vector3.right, out RaycastHit hitR, _stats.WallDetectionDistance, _stats.ClimbableLayer);
            bool hitLeft = Physics.CapsuleCast(point1, point2, checkRadius, Vector3.left, out RaycastHit hitL, _stats.WallDetectionDistance, _stats.ClimbableLayer);

            if (!hitRight && !hitLeft)
            {
                Vector3 rightOffset = Vector3.right * (_stats.WallDetectionDistance + 0.01f);
                Vector3 leftOffset = Vector3.left * (_stats.WallDetectionDistance + 0.01f);
                hitRight = Physics.CheckCapsule(point1 + rightOffset, point2 + rightOffset, checkRadius, _stats.ClimbableLayer);
                hitLeft = Physics.CheckCapsule(point1 + leftOffset, point2 + leftOffset, checkRadius, _stats.ClimbableLayer);
                if (hitRight)
                {
                    hitR = default;
                    hitR.normal = Vector3.left;
                }
                else if (hitLeft)
                {
                    hitL = default;
                    hitL.normal = Vector3.right;
                }
            }

            if (hitRight || hitLeft)
            {
                _isWallSliding = true;
                _wallHitNormal = hitRight ? hitR.normal : hitL.normal; // [����] �ؼ�����¼ǽ�ڵķ��߷���
            }
        }


        #endregion

        #region Jumping

        private bool _jumpToConsume;
        private bool _bufferedJumpUsable;
        private bool _endedJumpEarly;
        private bool _coyoteUsable;
        private float _timeJumpWasPressed = float.MinValue;

        private bool HasBufferedJump => _bufferedJumpUsable && _time < _timeJumpWasPressed + _stats.JumpBuffer;
        private bool CanUseCoyote => _coyoteUsable && !_grounded && _time < _frameLeftGrounded + _stats.CoyoteTime;

        private void HandleJump()
        {
            if (!_endedJumpEarly && !_grounded && !_frameInput.JumpHeld && _rb.linearVelocity.y > 0) _endedJumpEarly = true;

            if (!_jumpToConsume && !HasBufferedJump) return;

            // [����] ��ǽ�����ȼ�������ͨ��Ծ
            if (_isWallSliding && _jumpToConsume)
            {
                ExecuteWallJump();
                _jumpToConsume = false;
                return;
            }

            if (_grounded || CanUseCoyote) ExecuteJump();

            _jumpToConsume = false;
        }

        private void ExecuteJump()
        {
            _endedJumpEarly = false;
            _timeJumpWasPressed = 0;
            _bufferedJumpUsable = false;
            _coyoteUsable = false;
            _frameVelocity.y = _stats.JumpPower;
            Jumped?.Invoke();
        }



        private void ExecuteWallJump() // [����] ��ǽ��
        {
            _endedJumpEarly = false;
            _bufferedJumpUsable = false;
            _timeJumpWasPressed = 0;

            // 1. �����߼����ط��ߵ��� + ���ϵ���
            // ʹ�÷���(Normal)����ˮƽ���ȣ�����Vector3.up���Դ�ֱ����
            Vector3 jumpDir = _wallHitNormal * _stats.WallJumpHorizontalPower;
            jumpDir.y = _stats.WallJumpVerticalPower;

            _frameVelocity = jumpDir;

            // 2. �ؼ������������˳���ǽ״̬
            // ����������У�HandleDirection����ͬһ֡�ڼ�⵽ isWallSliding Ϊ�棬
            // �Ӷ������Ǹո�ֵ�� X/Z �ٶ�ǿ�ƹ��㡣
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
            if (_springAssistTime > 0f)
            {
                _springAssistTime -= Time.fixedDeltaTime;
                Vector3 dir = _springDirection.sqrMagnitude > 0f ? _springDirection.normalized : Vector3.up;
                float currentUpSpeed = Vector3.Dot(_frameVelocity, dir);
                if (currentUpSpeed > 0f)
                {
                    float targetUpSpeed = Mathf.MoveTowards(
                        currentUpSpeed,
                        _springMaxUpSpeed,
                        _springUpAcceleration * Time.fixedDeltaTime
                    );
                    _frameVelocity += dir * (targetUpSpeed - currentUpSpeed);
                }
            }

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

        private void LockZAxis()
        {
            _frameVelocity.z = 0f;
            if (_rb != null)
            {
                Vector3 v = _rb.linearVelocity;
                v.z = 0f;
                _rb.linearVelocity = v;

                Vector3 p = _rb.position;
                p.z = _lockedZ;
                _rb.position = p;
            }
        }

        public void AddFrameVelocity(Vector3 velocityChange, bool resetVelocity = false, float ungroundTime = 0.1f)
        {
            if (resetVelocity) _frameVelocity = Vector3.zero;
            _frameVelocity += velocityChange;

            if (velocityChange.y > 0f && ungroundTime > 0f)
            {
                _forceUngroundTime = Mathf.Max(_forceUngroundTime, _time + ungroundTime);
            }
        }

        public void SetVelocityAlongDirection(Vector3 direction, float speed, float ungroundTime = 0.1f)
        {
            Vector3 dir = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.up;
            float current = Vector3.Dot(_frameVelocity, dir);
            _frameVelocity += dir * (speed - current);

            if (ungroundTime > 0f)
            {
                _forceUngroundTime = Mathf.Max(_forceUngroundTime, _time + ungroundTime);
            }
            Debug.Log($"[PlayerController0] SetVelocityAlongDirection speed={speed:F2} unground={ungroundTime:F2}", this);
        }

        public void ZeroZVelocity()
        {
            _frameVelocity.z = 0f;
            if (_rb != null)
            {
                Vector3 v = _rb.linearVelocity;
                v.z = 0f;
                _rb.linearVelocity = v;
            }
        }

        public void ApplySpringImpulse(
            Vector3 direction,
            float force,
            float maxUpSpeed,
            float upAcceleration,
            float assistDuration,
            float ungroundTime,
            bool resetVelocity = true)
        {
            Vector3 dir = direction.sqrMagnitude > 0f ? direction.normalized : Vector3.up;
            _springDirection = dir;
            AddFrameVelocity(dir * force, resetVelocity, ungroundTime);

            if (ungroundTime > 0f)
            {
                _forceUngroundTime = Mathf.Max(_forceUngroundTime, _time + ungroundTime);
            }

            if (maxUpSpeed > 0f)
            {
                float currentUpSpeed = Vector3.Dot(_frameVelocity, dir);
                if (currentUpSpeed > maxUpSpeed)
                {
                    _frameVelocity -= dir * (currentUpSpeed - maxUpSpeed);
                }
            }

            if (assistDuration > 0f && upAcceleration > 0f)
            {
                _springMaxUpSpeed = maxUpSpeed;
                _springUpAcceleration = upAcceleration;
                _springAssistTime = Mathf.Max(_springAssistTime, assistDuration);
            }
            Debug.Log($"[PlayerController0] ApplySpringImpulse force={force:F2} maxUp={maxUpSpeed:F2} unground={ungroundTime:F2}", this);
        }

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
            // (����֮ǰ�ĵ��Դ��벻��)
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
                // [��������] �������߷��򣬷��㿴��ǽ���ķ���
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