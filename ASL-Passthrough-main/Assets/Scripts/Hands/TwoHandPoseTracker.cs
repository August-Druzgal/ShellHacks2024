using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Events;

public class TwoHandPoseTracker : MonoBehaviour
{
    public HandPoseTracker leftTracker;
    public HandPoseTracker rightTracker;

    [SerializeField] List<TwoHandGesture> twoHandGestureList = new List<TwoHandGesture>();

    public UnityEvent<Gesture> OnGestureEnter;

    private void Update()
    {
        rightTracker.GetHandPose();
        leftTracker.GetHandPose();

        rightTracker.UpdateStack();
        leftTracker.UpdateStack();

        //Check two handed poses
        if(!CheckTwoHandGesture())
        {
            //If no two handed poses
            rightTracker.CheckSingleHandGesture();
            leftTracker.CheckSingleHandGesture();
        }

        rightTracker.UpdateDisplayHandPose();
        leftTracker.UpdateDisplayHandPose();
    }

    /// <summary>
    /// Check if the recent hand poses from both hands match any known two handed gestures
    /// </summary>
    /// <returns></returns>
    bool CheckTwoHandGesture()
    {
        bool match = false;

        foreach (TwoHandGesture twoHandGesture in twoHandGestureList)
        {
            match = twoHandGesture.MatchGesture(leftTracker, rightTracker);

            if (match)
            {
                OnGestureEnter?.Invoke(twoHandGesture);
                leftTracker.ClearStack();
                rightTracker.ClearStack();
                return true;
            }
        }

        return false;
    }
}
