using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrontendDebugger : MonoBehaviour
{
    public SpawnLetterImages spawnLetterImages;

    public GameObject boxPrefab;
    public float offset = 0.7f;

    public void spawnFrame(string label, GameObject boundBox)
    {
        Vector3 offsetPosition = Camera.main.transform.position + Vector3.Scale(Camera.main.transform.localEulerAngles, new Vector3(offset, offset, offset));
        GameObject TemplateFrame = Instantiate(boxPrefab, boundBox.transform.position, Quaternion.identity);
        TemplateFrame.transform.localScale = boundBox.transform.localScale;
        // Debug.Log("Camera Posy: " + Camera.main.transform.position);
        // Debug.Log("Frame Posy: " + offsetPosition);

        // Should be the max of a function of TF length or minimum hardcoded visible scale
        float cwidth = 0.25f;
        float coffset = (label.Length * cwidth + TemplateFrame.transform.localScale.x) / 2 - cwidth;
        spawnLetterImages.spawnImages(label, TemplateFrame);

        foreach (char c in label)
        {
            GameObject preffyab = Resources.Load(c + " Variant") as GameObject;
            GameObject SignFrame = Instantiate(preffyab, this.transform);
            SignFrame.transform.parent = TemplateFrame.transform;
            SignFrame.transform.localScale = new Vector3(cwidth, cwidth, 0.15f);
            SignFrame.transform.localPosition = new Vector3(coffset, 0.62f, 0f);
            coffset -= cwidth;
        }
    }
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            var posit = Camera.main.transform.position;

            Debug.Log("Normal Rotations");
            Debug.Log(Camera.main.transform.rotation.x * 180 + "deg");
            Debug.Log(Camera.main.transform.rotation.y * 180 + "deg");
            Debug.Log(Camera.main.transform.rotation.z * 180 + "deg");
            Debug.Log("Spacer");
            Debug.Log(posit.x + ", " + posit.y + ", " + posit.z);

        }
        else if (Input.GetKeyDown(KeyCode.F))
        {

        }
    }
}
