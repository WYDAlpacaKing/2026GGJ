using UnityEngine;

public class Spike : MonoBehaviour
{

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log("Player hit spike! GameOver");
        }
    }
}
