using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandSystem : MonoBehaviour
{
    [SerializeField] HandPoseTracker leftHandTracker;
    [SerializeField] HandPoseTracker rightHandTracker;

    public static HandPoseTracker LeftHandTracker;
    public static HandPoseTracker RightHandTracker;

    private void Start()
    {
        LeftHandTracker = leftHandTracker;
        RightHandTracker = rightHandTracker;
    }
}
