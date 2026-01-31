using UnityEngine;

[CreateAssetMenu(menuName = "GGJ/Config/Mouse Follow Config", fileName = "MouseFollowConfig")]
public class MouseFollowConfig : ScriptableObject
{
    public enum MoveMode
    {
        MouseFollow,
        Keyboard
    }

    [Header("Mode")]
    public MoveMode MoveModeValue = MoveMode.MouseFollow;

    [Header("Mouse Follow")]
    public LayerMask GroundLayer;
    public float MouseLerpSpeed = 15f;
    public bool MouseUseSmoothing = true;

    [Header("Keyboard Move")]
    public float KeyboardMoveSpeed = 5f;
    public float KeyboardLerpSpeed = 15f;
    public bool KeyboardUseSmoothing = true;
}
