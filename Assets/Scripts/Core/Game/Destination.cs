using UnityEngine;

public class Destination : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Destination reached!");
            // Implement level completion logic here
            SceneTransitionManager.Instance.LoadNextLevel();
        }
    }
}
