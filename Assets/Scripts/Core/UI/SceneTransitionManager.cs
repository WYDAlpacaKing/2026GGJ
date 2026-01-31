using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening; // 引入 DOTween 命名空间

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance;

    [Header("UI 组件")]
    public CanvasGroup gameUICanvasGroup; // 游戏里的主UI，用于过场时渐隐
    public RectTransform circleWipe;      // 那个圆形的黑色 Image
    public Image blackBackground;         // 背景黑底（可选，用于叠底）

    [Header("设置")]
    public float duration = 1f;           // 动画时长
    public float maxScale = 25f;          // 圆形放大多少倍能盖住全屏 (根据屏幕适配调整)

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // 初始化状态：圆圈设为0，背景全透明
        if (circleWipe) circleWipe.localScale = Vector3.zero;
        if (blackBackground) blackBackground.color = new Color(0, 0, 0, 0);
    }

    /// <summary>
    /// 加载下一关
    /// </summary>
    public void LoadNextLevel()
    {
        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextIndex >= SceneManager.sceneCountInBuildSettings) nextIndex = 0;

        StartCoroutine(TransitionSequence(nextIndex));
    }

    /// <summary>
    /// 重新开始当前关卡
    /// </summary>
    public void RestartLevel()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        StartCoroutine(TransitionSequence(currentIndex));
    }

    // 使用协程来驱动 DOTween 的 Sequence，方便逻辑管理
    private System.Collections.IEnumerator TransitionSequence(int targetSceneIndex)
    {
        // 1. 创建 DOTween 序列
        Sequence seq = DOTween.Sequence();

        // --- 进场动画 ---

        // A. 暂停游戏逻辑 (防止玩家在转场时瞎动)
        seq.AppendCallback(() => {
            // PlayerController.Instance.SetInput(false); 
        });

        // B. 游戏UI渐隐 (如果有引用的话)
        if (gameUICanvasGroup != null)
        {
            seq.Join(gameUICanvasGroup.DOFade(0, 0.5f));
        }

        // C. 黑色圆圈从小变大 -> 遮住屏幕
        // SetEase 设为 InOutQuad 会比较顺滑
        seq.Append(circleWipe.DOScale(maxScale, duration).SetEase(Ease.InOutQuad));

        // D. 顺便把背景也弄黑，防止圆圈边缘有缝隙
        if (blackBackground)
        {
            seq.Join(blackBackground.DOFade(1, duration));
        }

        // --- 等待 Sequence 播放完进场动画 ---
        yield return seq.WaitForCompletion();

        // --- 遮挡期间的操作 (最关键的一步) ---

        // 2. 加载场景 / 重置位置
        // 直接 Reload Scene 是最彻底的“复原物体位置”方法
        AsyncOperation op = SceneManager.LoadSceneAsync(targetSceneIndex);
        op.allowSceneActivation = false;

        // 等待加载完成（此时屏幕是全黑的）
        while (op.progress < 0.9f) yield return null;
        op.allowSceneActivation = true;

        // 等一帧让 Start 方法执行
        yield return null;

        // 【扩展】如果你不是 Reload 场景，而是想手动复原物体位置，代码写在这里：
        // FindObjectOfType<PlayerController>().ResetPosition();

        // 重新获取新场景的 UI CanvasGroup (因为场景加载后引用会丢失)
        // 这一步需要你的游戏架构支持，比如 GameObject.Find 或者单例
        // SetupNewSceneUI(); 

        // --- 出场动画 ---

        Sequence seqOut = DOTween.Sequence();

        // E. 黑色圆圈从大变小 -> 露出屏幕
        seqOut.Append(circleWipe.DOScale(0, duration).SetEase(Ease.OutQuad));

        // F. 背景淡出
        if (blackBackground)
        {
            seqOut.Join(blackBackground.DOFade(0, duration));
        }

        // G. 游戏UI淡入
        // 注意：这里需要你重新获取了新场景的 gameUICanvasGroup 才能生效
        // seqOut.Join(newGameUI.DOFade(1, 0.5f));

        // H. 恢复输入
        seqOut.OnComplete(() => {
            // PlayerController.Instance.SetInput(true);
        });
    }
}