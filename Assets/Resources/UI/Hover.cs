using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;

public class TMPHoverTween : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public TextMeshProUGUI tmp;

    [Header("Color (instant)")]
    public Color normal = Color.white;
    public Color hover = new Color(1f, 0.9f, 0.3f);

    [Header("Scale (tween)")]
    public float hoverScale = 1.1f;
    public float duration = 0.15f;

    Vector3 _baseScale;
    Tween _scaleTween;

    void Awake()
    {
        if (!tmp) tmp = GetComponentInChildren<TextMeshProUGUI>();
        _baseScale = tmp.rectTransform.localScale;
        tmp.color = normal;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        tmp.color = hover; 

        _scaleTween?.Kill();
        _scaleTween = tmp.rectTransform
            .DOScale(_baseScale * hoverScale, duration)
            .SetEase(Ease.OutBack);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        tmp.color = normal; 

        _scaleTween?.Kill();
        _scaleTween = tmp.rectTransform
            .DOScale(_baseScale, duration)
            .SetEase(Ease.OutQuad);
    }
}
