using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    Camera cam;

    // Start is called before the first frame update
    void Awake()
    {
        cam = Camera.main;  
    }

    // Update is called once per frame
    void Update()
    {
        var halfCam = Camera.main.transform.position;
        halfCam.y /= 1f;
        //transform.LookAt(halfCam);
        transform.LookAt(Camera.main.transform);
    }
}
