using UnityEngine;

[CreateAssetMenu(menuName = "GGJ/Config/Charged Spring Config", fileName = "ChargedSpringConfig")]
public class ChargedSpringConfig : ScriptableObject
{
    public enum ReleaseMode
    {
        ImpulseForce,
        SetVelocity
    }

    [Header("Spring")]
    [Tooltip("压缩到最大（最低位置）所需时间，<=0 表示立刻满压缩")]
    public float CompressToMaxTime = 0.6f;
    [Tooltip("弹力系数：数值越大，释放时给的力/速度越强")]
    public float ForceCoefficient = 30f;
    [Range(0f, 1f)]
    [Tooltip("最大压缩比例（0~1），1 表示模型可压缩到 0 高度")]
    public float MaxCompressionRatio = 0.3f;
    [Tooltip("视觉回正速度（只影响外观，不影响逻辑）")]
    public float VisualReturnSpeed = 8f;
    [Tooltip("释放方式：ImpulseForce=施加冲量，SetVelocity=设置速度")]
    public ReleaseMode ReleaseForceMode = ReleaseMode.ImpulseForce;
    [Tooltip("释放后持续检测玩家的时间窗口，避免错过触发")]
    public float ReleaseWindowTime = 0.15f;
    [Tooltip("输出调试日志")]
    public bool DebugLog;

    [Header("Air Control (After Spring)")]
    [Tooltip("弹起后沿弹力方向的最大速度上限")]
    public float MaxUpSpeed = 12f;
    [Tooltip("弹起后沿弹力方向的加速度")]
    public float UpAcceleration = 60f;
    [Tooltip("弹起后额外上升辅助的持续时间")]
    public float AssistDuration = 0.2f;
    [Tooltip("强制离地时间，防止地面逻辑立刻压回")]
    public float UngroundTime = 0.1f;

    [Header("Legacy (Unused)")]
    [Min(1)]
    [Tooltip("旧阶段系统参数（已不使用）")]
    public int StageCount = 3;
    [Tooltip("旧阶段系统参数（已不使用）")]
    public float[] StageReleaseForces = { 4f, 8f, 12f };
    [Tooltip("旧阶段系统参数（已不使用）")]
    public Gradient[] StageGradients;
}
