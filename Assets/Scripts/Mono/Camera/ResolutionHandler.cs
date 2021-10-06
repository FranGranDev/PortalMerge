using UnityEngine;

public class ResolutionHandler : MonoBehaviour
{
    private Camera _camera;

    private void Awake()
    {
        _camera = Camera.main;
        SetFieldOfView();
    }

    private void SetFieldOfView()
    {
        float screenRatio = (1.0f * Screen.height) / (1.0f * Screen.width);
        if (1.7f < screenRatio && screenRatio < 1.8f)
        {
            _camera.fieldOfView = 60;
        }
        if (2.1f < screenRatio && screenRatio < 2.2f)
        {
            _camera.fieldOfView = 75;
        }
    }
}
