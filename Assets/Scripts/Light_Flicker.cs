using UnityEngine;
using System.Collections;

public class EmergencyLightFlicker : MonoBehaviour
{
    public Light emergencyLight;
    public Renderer emissiveRenderer;

    public float minWaitTime = 3f;
    public float maxWaitTime = 10f;

    public float minFlickerSpeed = 0.05f;
    public float maxFlickerSpeed = 0.3f;

    public int minFlickers = 2;
    public int maxFlickers = 5;

    private float originalIntensity;
    private Material emissiveMaterial;
    private Color originalEmission;

    void Start()
    {
        if (emergencyLight == null)
            emergencyLight = GetComponentInChildren<Light>();

        if (emissiveRenderer == null)
        {
            Transform child = transform.Find("Light_Emissive");

            if (child != null)
                emissiveRenderer = child.GetComponent<Renderer>();
        }

        if (emergencyLight == null || emissiveRenderer == null)
        {
            Debug.LogWarning("Missing light or emissive renderer!");
            enabled = false;
            return;
        }

        originalIntensity = emergencyLight.intensity;

        // Create a separate material instance
        emissiveMaterial = emissiveRenderer.material;
        originalEmission = emissiveMaterial.GetColor("_EmissionColor");

        StartCoroutine(FlickerLight());
    }

    IEnumerator FlickerLight()
    {
        while (true)
        {
            // Wait a random amount of time
            float waitTime = Random.Range(minWaitTime, maxWaitTime);
            yield return new WaitForSeconds(waitTime);

            // Choose a random number of flickers
            int flickers = Random.Range(minFlickers, maxFlickers + 1);

            for (int i = 0; i < flickers; i++)
            {
                // Turn light OFF
                emergencyLight.intensity = 0f;
                emissiveMaterial.SetColor(
                    "_EmissionColor", Color.black
                );

                // Random OFF duration
                float offTime = Random.Range(
                    minFlickerSpeed, maxFlickerSpeed
                );
                yield return new WaitForSeconds(offTime);

                // Turn light ON
                emergencyLight.intensity = originalIntensity;
                emissiveMaterial.SetColor(
                    "_EmissionColor", originalEmission
                );

                // Random ON duration
                float onTime = Random.Range(
                    minFlickerSpeed, maxFlickerSpeed
                );
                yield return new WaitForSeconds(onTime);
            }
        }
    }

    void OnDisable()
    {
        if (emergencyLight != null)
            emergencyLight.intensity = originalIntensity;

        if (emissiveMaterial != null)
            emissiveMaterial.SetColor(
                "_EmissionColor", originalEmission
            );
    }

    void OnDestroy()
    {
        if (emissiveMaterial != null)
            Destroy(emissiveMaterial);
    }
}
