using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening; // ���� DOTween �����ռ�

public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance;

    [Header("UI ���")]
    public CanvasGroup gameUICanvasGroup; // ��Ϸ�����UI�����ڹ���ʱ����
    public RectTransform circleWipe;      // �Ǹ�Բ�εĺ�ɫ Image
    public Image blackBackground;         // �����ڵף���ѡ�����ڵ��ף�

    [Header("����")]
    public float duration = 1f;           // ����ʱ��
    public float maxScale = 25f;          // Բ�ηŴ���ٱ��ܸ�סȫ�� (������Ļ�������)

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

        // ��ʼ��״̬��ԲȦ��Ϊ0������ȫ͸��
        if (circleWipe) circleWipe.localScale = Vector3.zero;
        if (blackBackground) blackBackground.color = new Color(0, 0, 0, 0);
    }

    /// <summary>
    /// ������һ��
    /// </summary>
    public void LoadNextLevel()
    {
        int nextIndex = SceneManager.GetActiveScene().buildIndex + 1;
        if (nextIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning("Next level not found in Build Settings. Reloading current level instead.");
            RestartLevel();
            return;
        }

        StartCoroutine(TransitionSequence(nextIndex));
    }

    public void LoadSpecificScene(int level)
    {
        //如果level并不存在则不执行
        if (level < 0 || level >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning("Level not found in Build Settings. Load skipped.");
            return;
        }
        StartCoroutine(TransitionSequence(level));
    }

    public void RestartLevel()
    {
        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        if (currentIndex < 0 || currentIndex >= SceneManager.sceneCountInBuildSettings)
        {
            Debug.LogWarning("Current level not found in Build Settings. Restart skipped.");
            return;
        }
        StartCoroutine(TransitionSequence(currentIndex));
    }

    // ʹ��Э�������� DOTween �� Sequence�������߼�����
    private System.Collections.IEnumerator TransitionSequence(int targetSceneIndex)
    {
        // 1. ���� DOTween ����
        Sequence seq = DOTween.Sequence();

        // --- �������� ---

        // A. ��ͣ��Ϸ�߼� (��ֹ�����ת��ʱϹ��)
        seq.AppendCallback(() => {
            // PlayerController.Instance.SetInput(false); 
        });

        // B. ��ϷUI���� (��������õĻ�)
        if (gameUICanvasGroup != null)
        {
            seq.Join(gameUICanvasGroup.DOFade(0, 0.5f));
        }

        // C. ��ɫԲȦ��С��� -> ��ס��Ļ
        // SetEase ��Ϊ InOutQuad ��Ƚ�˳��
        seq.Append(circleWipe.DOScale(maxScale, duration).SetEase(Ease.InOutQuad));

        // D. ˳��ѱ���ҲŪ�ڣ���ֹԲȦ��Ե�з�϶
        if (blackBackground)
        {
            seq.Join(blackBackground.DOFade(1, duration));
        }

        // --- �ȴ� Sequence ������������� ---
        yield return seq.WaitForCompletion();

        // --- �ڵ��ڼ�Ĳ��� (��ؼ���һ��) ---

        // 2. ���س��� / ����λ��
        // ֱ�� Reload Scene ����׵ġ���ԭ����λ�á�����
        AsyncOperation op = SceneManager.LoadSceneAsync(targetSceneIndex);
        op.allowSceneActivation = false;

        // �ȴ�������ɣ���ʱ��Ļ��ȫ�ڵģ�
        while (op.progress < 0.9f) yield return null;
        op.allowSceneActivation = true;

        // ��һ֡�� Start ����ִ��
        yield return null;

        // ����չ������㲻�� Reload �������������ֶ���ԭ����λ�ã�����д�����
        // FindObjectOfType<PlayerController>().ResetPosition();

        // ���»�ȡ�³����� UI CanvasGroup (��Ϊ�������غ����ûᶪʧ)
        // ��һ����Ҫ�����Ϸ�ܹ�֧�֣����� GameObject.Find ���ߵ���
        // SetupNewSceneUI(); 

        // --- �������� ---

        Sequence seqOut = DOTween.Sequence();

        // E. ��ɫԲȦ�Ӵ��С -> ¶����Ļ
        seqOut.Append(circleWipe.DOScale(0, duration).SetEase(Ease.OutQuad));

        // F. ��������
        if (blackBackground)
        {
            seqOut.Join(blackBackground.DOFade(0, duration));
        }

        // G. ��ϷUI����
        // ע�⣺������Ҫ�����»�ȡ���³����� gameUICanvasGroup ������Ч
        // seqOut.Join(newGameUI.DOFade(1, 0.5f));

        // H. �ָ�����
        seqOut.OnComplete(() => {
            // PlayerController.Instance.SetInput(true);
        });
    }
}