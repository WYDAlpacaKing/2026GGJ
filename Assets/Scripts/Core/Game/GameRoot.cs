using Alpaca.Game.UI;
using UnityEngine;

public class GameRoot : MonoBehaviour
{
    void Start()
    {
        UIManager.Instance.OpenPanel("MainPanel");
    }
}
