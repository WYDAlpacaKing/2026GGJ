using UnityEngine;

public class TMPBreathSwing : MonoBehaviour
{
    [Header("可调参数")]
    public float rotateAmount = 4f;    
    public float rotateOffset = 0f;    
    public float breathAmount = 0.05f;  
    public float speed = 2f;

    public bool clockwise = true;

    RectTransform rect;
    Vector3 baseScale;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
        baseScale = rect.localScale;
    }

    void Update()
    {
        float t = Time.unscaledTime * speed;

        int dir = clockwise ? 1 : -1;

        // 旋转
        float angle = rotateOffset + Mathf.Sin(t) * rotateAmount * dir;
        rect.localRotation = Quaternion.Euler(0, 0, angle);

        // 呼吸
        float scale = 1f + Mathf.Sin(t) * breathAmount;
        rect.localScale = baseScale * scale;
    }
}
