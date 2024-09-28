using Oculus.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ThrowSpell : MonoBehaviour
{
    Grabbable grabbable;

    int lastFramePointCount = 0;

    public UnityEvent OnGrab;
    public UnityEvent OnRelease;

    Transform palmTrans;

    Rigidbody rb;

    [SerializeField] float throwForce = 10f;
    [SerializeField] float maxDist = 20f;

    private void OnEnable()
    {
        rb = GetComponent<Rigidbody>();
        grabbable = GetComponent<Grabbable>();
        OnGrab.AddListener(GrabSpell);
        OnRelease.AddListener(SendSpell);
    }

    private void OnDisable()
    {
        OnGrab.RemoveListener(GrabSpell);
        OnRelease.RemoveListener(SendSpell);
    }

    private void Update()
    {
        //Destroy object if far away
        if(Vector3.Distance(Camera.main.transform.position, transform.position) > maxDist)
        {
            Destroy(gameObject);
        }

        if(grabbable.PointsCount > 0 && //Grabbed
            lastFramePointCount <= 0)   //Last fame was not grabbed
        {
            OnGrab?.Invoke();
        }
        else if (grabbable.PointsCount <= 0 && //Not grabbed
            lastFramePointCount > 0)   //Last fame was grabbed
        {
            OnRelease?.Invoke();
        }

        lastFramePointCount = grabbable.PointsCount;
    }

    void GrabSpell()
    {
        if (Vector3.Distance(HandSystem.LeftHandTracker.GetPalmTransform().position, transform.position) <
            Vector3.Distance(HandSystem.RightHandTracker.GetPalmTransform().position, transform.position))
        {
            palmTrans = HandSystem.LeftHandTracker.GetPalmTransform();
        }
        else
        {
            palmTrans = HandSystem.RightHandTracker.GetPalmTransform();
        }

        transform.position = palmTrans.position;
    }

    void SendSpell()
    {
        rb.AddForce(-palmTrans.up * throwForce);
    }
}
