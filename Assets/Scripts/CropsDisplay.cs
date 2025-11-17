using IoT;
using Newtonsoft.Json.Linq;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class CropsDisplay : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] TextMeshProUGUI label;

    void Start()
    {
        //OneM2M.AddValueRefreshEvent(Mirror);
    }



    public void SetText(string content, int count)
    {
        //Debug.Log(count+":"+content);
        if(count == 0)
        {
            content = "비어있음";
            count = 1;
        }

        GetComponent<RectTransform>().sizeDelta = new Vector2(524.2252f, 82f+count * 90f);
        label.text = content;
    }
}
