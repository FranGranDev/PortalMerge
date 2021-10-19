using UnityEngine;

public class ResolutionHandler : MonoBehaviour
{
    public static ResolutionHandler Active;

    public ResolutionHandler()
    {
        Active = this;
    }

    private void Awake()
    {
        SetFieldOfView();
    }

    public void SetFieldOfView()
    {
        float screenRatio = (1.0f * Screen.height) / (1.0f * Screen.width);
        if (1.7f < screenRatio && screenRatio < 1.8f)
        {
            Camera.main.fieldOfView = 60;
        }
        if (2.1f < screenRatio && screenRatio < 2.2f)
        {
            Camera.main.fieldOfView = 75;
        }
    }
}
