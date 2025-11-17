using IoT;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem.HID;
using UnityEngine.UIElements;

public class Crops : MonoBehaviour
{
    // Start is called before the first frame update
    void Awake()
    {
        speicesPrefab = new GameObject[Enum.GetValues(typeof(ESpecies)).Length];
        speicesPrefab[(int)ESpecies.poinsettia] = prefabPoinsenttia;
        speicesPrefab[(int)ESpecies.basil] = prefabBasil;
        //Debug.Log(speicesPrefab[(int)ESpecies.poinsenttia]);


        cropList = new List<Crop>();


        foreach(Transform child in cropModelParent.transform)
        {
            
            cropList.Add(new Crop()
            {
                gameObject = child.gameObject
            });

            for(int i=0; i<child.childCount; i++)
            {
                Destroy(child.GetChild(i).gameObject);
            }
        }
        

    }

    // Update is called once per frame
    void Start()
    {
        
        OneM2M.AddValueRefreshEvent(Mirror);
    }

    GameObject[] speicesPrefab;
    List<Crop> cropList;
    string cropsData = "";

    //List<GameObject> cropList;
    [SerializeField] GameObject cropModelParent;
    [SerializeField] GameObject prefabPoinsenttia;
    [SerializeField] GameObject prefabBasil;
    [SerializeField] CropsDisplay cropsDisplay;

    class Crop
    {
        internal GameObject gameObject;
        bool isHealth;
        ESpecies species;

        public string SetValueAndGetDisplayContent(string text)
        {
            //string text = "healthy_basil";
            //this.position = position;
            int idx = text.IndexOf('_');
            string front = text.Substring(0, idx);
            string back = text.Substring(idx + 1);
            

            switch (front)
            {
                case "healthy":
                    isHealth = true;
                    break;
                case "unhealthy":
                    isHealth = false;
                    break;

                default:
                    Debug.LogError("Unexpected value input");
                    break;
            }

            foreach(ESpecies cropType in Enum.GetValues(typeof(ESpecies)))
            {
                if(cropType.ToString() == back)
                {
                    species = cropType;
                    Debug.Log(species);

                    string content = "";
                    content += ". " + cropType.ToString() + "\n    state : " + (isHealth ? "healthy" : "unhealthy")+"\n";
                    return content;
                }
            }

            Debug.LogError("Unexpected species input");
            return "error";
        }

        public void DestroyVisualCrop()
        {
            //시각적인 부분 다 죽이고
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                Destroy(gameObject.transform.GetChild(i).gameObject);
            }
        }

        public void MakeVisualCrop(GameObject[] speicesPrefab)
        {
            GameObject cropModel = Instantiate(speicesPrefab[(int)species], gameObject.transform);
            Debug.Log(species.ToString()+speicesPrefab[(int)species]);

            if (isHealth)
            {
                //Todo
                //cropModel이 건강해보이는 이펙트
                cropModel.GetComponent<MeshRenderer>().material.SetFloat("_UnHealthyStrangth", 0);
            }
            else
            {
                //안건강해보이는 이펙트
                cropModel.GetComponent<MeshRenderer>().material.SetFloat("_UnHealthyStrangth", 1);
            }
        }

        




    }


    enum ESpecies {
        poinsettia,
        basil
    }


    //상자 안에 점을 찍어서 위치 저장
    //


    void Mirror()
    {
        //Debug.Log(OneM2M.GetFarmParameter(OneM2M.EFarmParameter.health));
        JToken data = JObject.Parse(OneM2M.GetFarmParameter(OneM2M.EFarmParameter.health))["data"];
        string dataString = data.ToString();
        string displayContent = "";
        if (cropsData == dataString)
        {
            //데이터에 변경사항 없음
            return;
        }

        cropsData = dataString;
        int i = 0;
        for(; i< cropList.Count; i++)
        {
            JToken cropInfo = data[i.ToString()];
            if (cropInfo != null)
            {
                displayContent += (i+1) + cropList[i].SetValueAndGetDisplayContent(data[i.ToString()].ToString());
                cropList[i].DestroyVisualCrop();
                cropList[i].MakeVisualCrop(speicesPrefab);
            } else
            {
                i++;
                break;
            }

            
        }
        
        cropsDisplay.SetText(displayContent, i-1);

        for (; i < cropList.Count; i++)
        {
            //cropList[i].SetValue(data[i.ToString()].ToString());
            cropList[i].DestroyVisualCrop();
        }
    }
}
