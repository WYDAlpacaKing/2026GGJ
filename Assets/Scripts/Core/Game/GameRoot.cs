using Alpaca.Game.UI;
using UnityEngine;
using Alpaca.Game.Audio;


public class GameRoot : MonoBehaviour
{
    void Start()
    {
        UIManager.Instance.OpenPanel("MainPanel");
        MusicMgr.Instance.PlayBgMusic(AudioID.BGM_mainmenu);
    }
}
