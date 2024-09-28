using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MaterialSwap : MonoBehaviour
{
    [SerializeField] Material matOff, matOn, matRed;

    MeshRenderer meshRend;

    [SerializeField] List<HandPoseTracker> handPoseTrackers;
    [SerializeField] TwoHandPoseTracker twoHandPoseTracker;

    [SerializeField] string redPoseName = "";

    [SerializeField] float displayTime = 2f;
    float currentTime = 0;

    private void Start()
    {
        meshRend = GetComponent<MeshRenderer>();
        meshRend.material = matOff;
    }

    private void OnEnable()
    {
        foreach(HandPoseTracker tracker in handPoseTrackers)
        {
            tracker.OnGestureEnter.AddListener(OnEnter);
            //handPoseTracker.OnPoseExit.AddListener(OnExit);

            HandPose redPose = tracker.GetHandPose(redPoseName);

            if (redPose != null)
            {
                //redPose.OnPoseEnter.AddListener(OnEnterRed);
                //redPose.OnPoseExit.AddListener(OnExit);
            }
        }

        twoHandPoseTracker.OnGestureEnter.AddListener(OnEnter);
    }

    private void OnDisable()
    {
        foreach (HandPoseTracker tracker in handPoseTrackers)
        {
            tracker.OnGestureEnter.RemoveListener(OnEnter);
            //handPoseTracker.OnPoseExit.RemoveListener(OnExit);

            HandPose redPose = tracker.GetHandPose(redPoseName);

            if (redPose != null)
            {
                //redPose.OnPoseEnter.RemoveListener(OnEnterRed);
                //redPose.OnPoseExit.RemoveListener(OnExit);
            }
        }

        twoHandPoseTracker.OnGestureEnter.RemoveListener(OnEnter);
    }

    private void Update()
    {
        if (currentTime <= 0)
        {
            meshRend.material = matOff;
        }
        else
        {
            currentTime -= Time.deltaTime;
        }
    }

    public void OnEnter(Gesture gesture)
    {
        currentTime = displayTime;
        meshRend.material = matOn;
    }

    public void OnExit(HandPose pose)
    {
        meshRend.material = matOff;
    }

    public void OnEnterRed(HandPose pose)
    {
        currentTime = displayTime;
        meshRend.material = matRed;
    }
}
