using UnityEngine;

[RequireComponent(typeof(Collider))]
public class CameraZone : MonoBehaviour
{
    [SerializeField] private Camera zoneCamera;
    [SerializeField] private Camera fallbackCamera;
    [SerializeField] private Camera[] allCameras;

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player")) Show(zoneCamera);
    }

    private void OnTriggerExit(Collider other)
    {
        // Only revert if this zone's camera is still the one showing; a neighbouring zone may already have taken over.
        if(fallbackCamera != null && zoneCamera.enabled && other.CompareTag("Player")) Show(fallbackCamera);
    }

    private void Show(Camera cam)
    {
        foreach(var c in allCameras)
        {
            bool on = c == cam;
            c.enabled=on;
            var listener = c.GetComponent<AudioListener>();
            if(listener != null) listener.enabled=on;
        }
    }

}
