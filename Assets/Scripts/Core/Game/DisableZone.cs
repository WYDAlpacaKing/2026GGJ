using UnityEngine;

public class DisableZone : MonoBehaviour
{
    [SerializeField] private bool _disableOnEnter = true;

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out MouseFollow mouseFollow))
        {
            mouseFollow.SetCanReveal(!_disableOnEnter);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent(out MouseFollow mouseFollow))
        {
            mouseFollow.SetCanReveal(_disableOnEnter);
        }
    }
}
