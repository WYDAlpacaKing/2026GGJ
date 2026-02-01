using UnityEngine;
using UnityEngine.SceneManagement;

public class Destination : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Destination reached!");
            // 如果是最后一个场景 则回到序号为0的场景
            if(SceneManager.GetActiveScene().buildIndex == SceneManager.sceneCountInBuildSettings - 1)
            {
                SceneTransitionManager.Instance.LoadSpecificScene(0);
            }
            else
            {
                SceneTransitionManager.Instance.LoadNextLevel();
            }

        }
    }
}
