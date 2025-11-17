using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField] Vector3 doorOpenVector;
    [SerializeField] Vector3 doorCloseVector;
    bool doorIsOpen;

    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (doorIsOpen)
        {
            transform.localPosition = Vector3.LerpUnclamped(transform.localPosition, doorOpenVector, Time.deltaTime*30);
        } else
        {
            transform.localPosition = Vector3.LerpUnclamped(transform.localPosition, doorCloseVector, Time.deltaTime * 30);
        }

        
        
    }


    public void SetDoor(bool active)
    {
        doorIsOpen = active;
    }
}
