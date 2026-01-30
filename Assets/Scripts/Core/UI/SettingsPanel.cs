using Alpaca.Game.Audio;
using Alpaca.Game.UI;
using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : BasePanel
{
    [Header("--- UI 组件引用 ---")]
    [Tooltip("关闭/返回按钮")]
    [SerializeField] private Button btnClose;

    [Tooltip("背景音乐音量滑条")]
    [SerializeField] private Slider sliderBGM;

    [Tooltip("音效音量滑条")]
    [SerializeField] private Slider sliderSFX;

    // 如果你有文本显示音量数值的需求，可以在这里加 Text 引用

    /// <summary>
    /// 初始化：绑定事件监听
    /// </summary>
    public override void Init(params object[] args)
    {
        base.Init(args);

        // 1. 绑定关闭按钮
        btnClose.onClick.AddListener(() =>
        {
            PlayClickSound();
            UIManager.Instance.Back();
        });

        // 2. 绑定 BGM 滑条
        sliderBGM.onValueChanged.AddListener((value) =>
        {
            // 实时修改音量
            MusicMgr.Instance.ChangeBgValue(value);
        });

        // 3. 绑定音效滑条
        sliderSFX.onValueChanged.AddListener((value) =>
        {
            MusicMgr.Instance.ChangeSoundValue(value);
        });

    }

    /// <summary>
    /// 每次打开面板时：同步数据
    /// </summary>
    public override void OnOpen(params object[] args)
    {
        base.OnOpen(args);

        // --- 核心逻辑：数据回显 ---
        // 必须确保面板打开时，滑条的位置对应当前的真实音量
        // 否则会出现“滑条在100%，实际音量是50%”的 Bug

        sliderBGM.value = MusicMgr.Instance.MusicVolume;
        sliderSFX.value = MusicMgr.Instance.SoundVolume;
    }

    private void PlayClickSound()
    {
        // 播放通用的 UI 点击音效
        // MusicMgr.Instance.PlaySound(AudioID.SFX_UIClick);
    }
}
