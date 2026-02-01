using Alpaca.Game.Audio;
using Alpaca.Game.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainPanel : BasePanel
{
    [Header("--- UI ������� ---")]
    [SerializeField] private Button btnStart;
    [SerializeField] private Button btnSettings;
    [SerializeField] private Button btnQuit;
    private bool _waitingForScene;

    // ��д����� OnInit��ֻ�ڵ�һ�μ���ʱִ��
    public override void Init(params object[] args)
    {
        base.Init(args);

        // --- 1. �󶨿�ʼ��Ϸ ---
        btnStart.onClick.AddListener(() =>
        {
            PlayClickSound();
            StartGame();
        });

        // --- 2. ��������� ---
        // ������ʾ���Ĺ��ܣ����Ӵ�һ���µ����
        btnSettings.onClick.AddListener(() =>
        {
            PlayClickSound();
            // ��������壬ָ���� Normal �㣨���� Top �㣬����ƣ�
            UIManager.Instance.OpenPanel("SettingsPanel", UILayer.Normal);
        });

        // --- 3. ���˳���Ϸ ---
        btnQuit.onClick.AddListener(() =>
        {
            PlayClickSound();
            QuitGame();
        });
    }

    /// <summary>
    /// ÿ�δ����ʱ���� (��������ý��淵��ʱ�����߸�����ʱ)
    /// </summary>
    public override void OnOpen(params object[] args)
    {
        base.OnOpen(args);

        // �������˵� BGM
        // (�������� AudioID �ﶨ���� BGM_MainMenu)



        //MusicMgr.Instance.PlayBgMusic(AudioID.BGM_MainMenu);
    }

    // --- ҵ���߼� ---

    private void StartGame()
    {
        Debug.Log("��ʼ��Ϸ����...");
        MusicMgr.Instance.StopBgMusic();
        MusicMgr.Instance.PlayBgMusic(AudioID.BGM_playing);
        if (!_waitingForScene)
        {
            _waitingForScene = true;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        SceneManager.LoadScene("Art_L1");
        UIManager.Instance.Back();
        CloseSelf();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != "Art_L1") return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        _waitingForScene = false;
        GameManager.Instance.StartGame();
    }

    private void QuitGame()
    {
        Debug.Log("�˳���Ϸ");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    // ��������������ͨ�õ� UI �����Ч
    private void PlayClickSound()
    {
        // (�������� AudioID �ﶨ���� SFX_UIClick)
         //MusicMgr.Instance.PlaySound(AudioID.SFX_UIClick);
    }
}
