using Alpaca.Game.UI;
using UnityEngine;

public class GameManager : BaseMonoMgr<GameManager>
{
    public GameState CurrentState { get; private set; } = GameState.MainMenu;


    protected override void Awake()
    {
        base.Awake();
        DontDestroyOnLoad(gameObject);
    }
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            if(CurrentState == GameState.Playing)
            {
                PauseGame();
            }
            else if(CurrentState == GameState.Paused)
            {
                ResumeGame();
            }
        }
    }

    public void StartGame()
    {
        CurrentState = GameState.Playing;
    }

    public void PauseGame()
    {
        if (CurrentState == GameState.Playing)
        {
            CurrentState = GameState.Paused;
            Time.timeScale = 0f; // 暂停游戏时间
            UIManager.Instance.OpenPanel("PausePanel");
        }
    }

    public void ResumeGame()
    {
        if (CurrentState == GameState.Paused)
        {
            CurrentState = GameState.Playing;
            Time.timeScale = 1f; // 恢复游戏时间
            UIManager.Instance.Back(); // 关闭暂停面板
        }
    }

    public void ReturnToMainMenu()
    {
        CurrentState = GameState.MainMenu;
        Time.timeScale = 1f; // 确保时间恢复正常
    }
}

public enum GameState
{
    MainMenu,
    Playing,
    Paused,
}
