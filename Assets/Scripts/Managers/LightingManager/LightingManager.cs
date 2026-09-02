using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LightingManager : MonoBehaviour
{
    [SerializeField] private bool _ensureGlobalLightOnRun = true;
    [SerializeField] private Color _globalLightColor = Color.white;
    [SerializeField] private float _globalLightIntensity = 1f;
    [SerializeField] private string _globalLightName = "Global Light 2D";

    public Light2D GlobalLight { get; private set; }

    public void PrepareRunLighting()
    {
        if (_ensureGlobalLightOnRun)
            EnsureGlobalLight2D();
    }

    public Light2D EnsureGlobalLight2D()
    {
        if (GlobalLight == null)
            GlobalLight = FindGlobalLight2D();

        if (GlobalLight == null)
        {
            GameObject lightObject = new GameObject(_globalLightName);
            GlobalLight = lightObject.AddComponent<Light2D>();
            GlobalLight.lightType = Light2D.LightType.Global;
        }

        GlobalLight.color = _globalLightColor;
        GlobalLight.intensity = _globalLightIntensity;
        return GlobalLight;
    }

    private static Light2D FindGlobalLight2D()
    {
        Light2D[] lights = FindObjectsByType<Light2D>(FindObjectsSortMode.None);
        foreach (Light2D light in lights)
        {
            if (light != null && light.lightType == Light2D.LightType.Global)
                return light;
        }

        return null;
    }
}
