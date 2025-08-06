using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class FanSwitch : MonoBehaviour
{
    public TextMeshProUGUI buttonText;
    public Image buttonImage;

    private bool isOn = false;

    public void ToggleFan()
    {
        // true <-> false
        isOn = !isOn;

        if (isOn)
        {
            buttonText.text = "FAN ON";
            buttonImage.color = Color.green;
        }
        else
        {
            buttonText.text = "FAN OFF";
            buttonImage.color = Color.gray;
        }

        Debug.Log("Fan is now " + (isOn ? "ON" : "OFF"));
    }
}
