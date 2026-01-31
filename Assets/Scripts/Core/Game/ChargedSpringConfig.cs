using UnityEngine;

[CreateAssetMenu(menuName = "GGJ/Config/Charged Spring Config", fileName = "ChargedSpringConfig")]
public class ChargedSpringConfig : ScriptableObject
{
    [Header("Inactive")]
    public float InactiveBounceForce = 2f;

    [Header("Air Control (After Spring)")]
    public float MaxUpSpeed = 12f;
    public float UpAcceleration = 60f;
    public float AssistDuration = 0.2f;
    public float UngroundTime = 0.1f;

    [Header("Stages")]
    [Min(1)] public int StageCount = 3;
    public float[] StageReleaseForces = { 4f, 8f, 12f };

    [Header("Debug Colors (per stage)")]
    public Gradient[] StageGradients;
}
