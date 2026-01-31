using UnityEngine;

namespace TarodevController
{
    [CreateAssetMenu]
    public class ScriptableStats : ScriptableObject
    {
        [Header("LAYERS")]
        public LayerMask PlayerLayer;
        public LayerMask ClimbableLayer;

        [Header("INPUT")]
        public bool SnapInput = true;
        [Range(0.01f, 0.99f)] public float VerticalDeadZoneThreshold = 0.3f;
        [Range(0.01f, 0.99f)] public float HorizontalDeadZoneThreshold = 0.1f;

        [Header("MOVEMENT")]
        public float MaxSpeed = 14;
        public float Acceleration = 120;
        public float GroundDeceleration = 60;
        public float AirDeceleration = 30;
        [Range(0f, -10f)] public float GroundingForce = -1.5f;
        [Range(0f, 0.5f)] public float GrounderDistance = 0.05f;

        [Header("WALL SLIDE / JUMP")]
        public float WallSlideSpeed = 3f;
        public float WallDetectionDistance = 0.5f;

        // [新增] 蹬墙跳参数
        [Tooltip("Force applied away from the wall")]
        public float WallJumpHorizontalPower = 20f; // 水平蹬出的力度
        [Tooltip("Force applied upwards")]
        public float WallJumpVerticalPower = 30f;   // 垂直向上的力度

        [Header("JUMP")]
        public float JumpPower = 36;
        public float MaxFallSpeed = 40;
        public float FallAcceleration = 110;
        public float JumpEndEarlyGravityModifier = 3;
        public float CoyoteTime = .15f;
        public float JumpBuffer = .2f;
    }
}