using Alpaca.Game.Audio;
using Alpaca.Game.UI;
using UnityEngine;
using UnityEngine.UI;

public class MainPanel : BasePanel
{
    [Header("--- UI 组件引用 ---")]
    [SerializeField] private Button btnStart;
    [SerializeField] private Button btnSettings;
    [SerializeField] private Button btnQuit;

    // 重写基类的 OnInit，只在第一次加载时执行
    public override void Init(params object[] args)
    {
        base.Init(args);

        // --- 1. 绑定开始游戏 ---
        btnStart.onClick.AddListener(() =>
        {
            PlayClickSound();
            StartGame();
        });

        // --- 2. 绑定设置面板 ---
        // 这里演示核心功能：叠加打开一个新的面板
        btnSettings.onClick.AddListener(() =>
        {
            PlayClickSound();
            // 打开设置面板，指定在 Normal 层（或者 Top 层，看设计）
            UIManager.Instance.OpenPanel("SettingsPanel", UILayer.Normal);
        });

        // --- 3. 绑定退出游戏 ---
        btnQuit.onClick.AddListener(() =>
        {
            PlayClickSound();
            QuitGame();
        });
    }

    /// <summary>
    /// 每次打开面板时调用 (比如从设置界面返回时，或者刚启动时)
    /// </summary>
    public override void OnOpen(params object[] args)
    {
        base.OnOpen(args);

        // 播放主菜单 BGM
        // (假设你在 AudioID 里定义了 BGM_MainMenu)
        MusicMgr.Instance.PlayBgMusic(AudioID.BGM_MainMenu);
    }

    // --- 业务逻辑 ---

    private void StartGame()
    {
        Debug.Log("开始游戏流程...");

        CloseSelf();

        
    }

    private void QuitGame()
    {
        Debug.Log("退出游戏");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    // 辅助方法：播放通用的 UI 点击音效
    private void PlayClickSound()
    {
        // (假设你在 AudioID 里定义了 SFX_UIClick)
         MusicMgr.Instance.PlaySound(AudioID.SFX_UIClick);
    }
}
