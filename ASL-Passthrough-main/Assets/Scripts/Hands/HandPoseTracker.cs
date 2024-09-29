using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using Oculus.Interaction.Input;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.Events;
using static OVRPlugin;
using System;

/// <summary>
/// Data about a hand pose in a specific frame
/// </summary>
public struct HandPoseData
{
    public float startTime;         //Time at which this hand pose was first made
    public float elapsedTime;       //How long this pose has been held
    public float timeBetweenPoses;  //How long there was a null pose (no known hand pose) between this pose and the next (above this element in the stack)
    public HandPose pose;           //Pose being held
}

/// <summary>
/// Component for tracking hand positions on a single hand
/// </summary>
public class HandPoseTracker : MonoBehaviour
{
    [SerializeField] bool right = true;

    [SerializeField] public List<HandPose> handPoseList;
    [SerializeField] public List<HandGesture> handGestureList;

    List<HandPoseData> handPoseDataStack = new List<HandPoseData>();

    public IHand handCurrent;       //Hand script, RightHand under OVRHands
    public HandVisual handVisual;   //Visual under handCurrent

    [SerializeField] Transform palmTransform;

    HandPose currentPose = null;

    //Generic events fired for every pose
    public UnityEvent<HandPose> OnPoseEnter;
    public UnityEvent<HandPose> OnPoseStay;
    public UnityEvent<HandPose> OnPoseExit;

    //Generic events fired for every gesture
    public UnityEvent<HandGesture> OnGestureEnter = new();

    //Determines if hand is making a pose currently or not
    bool inPose = false;

    //In case we want a difficulty toggle requiring more precise or less precise signs
    float toleranceMultiplier = 1.2f;

    //Displayed hand pose
    HandPose displayPose;

    [SerializeField] GameObject handPositionMarkerPrefab;
    GameObject displayHandPositionMarker;

    [Header("Debug")]

    [SerializeField] bool debugDisplayHandPose = true;
    [SerializeField] float debugDisplayHandTime = 4f;
    float currentDebugDisplayHandTime = 0;

#if UNITY_EDITOR
    [Header("Editor")]

    [SerializeField] bool modifyEditorHandPose = true;

    //Only used for editor to save current hand pose to this scriptable object
    public HandPose currentEditorHandPose;
#endif

    public Transform GetPalmTransform() { return palmTransform; }
    public List<HandPoseData> GetStack() { return handPoseDataStack; }
    public void ClearStack() { handPoseDataStack.Clear(); }

    private void Start()
    {
        handVisual = GetComponentInParent<HandVisual>();
        handCurrent = handVisual.Hand;
    }

    private void OnEnable()
    {
        OnPoseEnter.AddListener(DisplayHandPose);
    }

    private void OnDisable()
    {
        OnPoseEnter.RemoveListener(DisplayHandPose);
    }

    private void Update()
    {
#if UNITY_EDITOR
        //Save current hand pose to prefab in currentEditorHandPose
        if (Input.GetKeyDown(KeyCode.Return))
        {
            Debug.Log("Return");
            EditorSaveHandPose();
        }
#endif

        //GetHandPose();

        ////Do nothing if current pose is null
        //if (currentPose != null)
        //{
        //    UpdateStack();

        //    CheckSingleHandGesture();
        //}

        //UpdateDisplayHandPose();
    }

    /// <summary>
    /// Checks current hand pose against list of known poses and sets currentPose to the current known pose or to null if there is no pose
    /// </summary>
    public void GetHandPose()
    {
        HandPose wantedHandPose = null;

        if (SpawnLetterImages.charQueue.Count >= 0)
        {
            wantedHandPose = handPoseList.Find(x => x.GetDisplayName() == SpawnLetterImages.charQueue.Peek().ToString());
        }

        // Log the wanted hand pose once

        // Get pose of current hand
        foreach (HandPose pose in handPoseList)
        {
            // Check if HandPose matches current hand
            if (pose.CheckHandMatch(handCurrent, toleranceMultiplier))
            {
                // OnEnter
                if (!pose.GetInPose())
                {
                    OnPoseEnter?.Invoke(pose);
                    inPose = true; // Hand tracker has logged a pose, general
                    pose.SetInPose(true); // This pose is the one that is logged

                    currentPose = pose;
                }

                // OnStay
                OnPoseStay?.Invoke(pose);
            }
            else if (pose.GetInPose()) // OnExit
            {
                OnPoseExit?.Invoke(pose);
                inPose = false;
                pose.SetInPose(false);

                currentPose = null;
            }
        }

        if (SpawnLetterImages.charQueue.Count >= 0)
        {
            Dictionary<string, float> dic = wantedHandPose.GetFingerTipIntensities(handCurrent, toleranceMultiplier);

            // Log the current hand pose once
            //handCurrent.GetJointPose(HandJointId.HandIndexTip, out Pose indexTipPose);
            //handCurrent.GetJointPose(HandJointId.HandMiddleTip, out Pose middleTipPose);
            //handCurrent.GetJointPose(HandJointId.HandRingTip, out Pose ringTipPose);
            //handCurrent.GetJointPose(HandJointId.HandPinkyTip, out Pose pinkyTipPose);
            //handCurrent.GetJointPose(HandJointId.HandThumbTip, out Pose thumbTipPose);

            //Debug.Log("Index: " + dic["Index"] + " Middle: " + dic["Middle"] + " Ring: " + dic["Ring"] + " Pinky: " + dic["Pinky"] + " Thumb: " + dic["Thumb"]);
        }
    }

    /// <summary>
    /// Updates stack based on current hand pose, current state of the stack, and delta time
    /// </summary>
    public void UpdateStack()
    {
        //Need to include null poses so we know for how long there was a pause between poses
        //if (currentPose == null) { return; }

        if (handPoseDataStack.Count <= 0)   //Stack is empty
        {
            //If pose is not null, add to stack
            if (currentPose != null)
            {
                AddNewStackElement();
            }

            //Else Do Nothing
        }
        else if (handPoseDataStack[0].pose != currentPose)   //or Current pose does not match top of stack, stack stored pose can never be null
        {
            if (currentPose == null)    //If pose is null, track time
            {
                //Pop off top element, increase time between poses, push back to stack
                HandPoseData data = handPoseDataStack[0];
                data.timeBetweenPoses += Time.deltaTime;
                handPoseDataStack[0] = data;
            }
            else    //If pose is not null but inequal, create a new stack element
            {
                AddNewStackElement();
            } 
        }
        else if (handPoseDataStack[0].pose == currentPose) //Current pose matches pose on top of stack
        {
            if(handPoseDataStack[0].timeBetweenPoses > 0)   //If any null space between this and last pose, create new stack element
            {
                AddNewStackElement();
            }
            else //Last frame was this pose and this frame was this pose, just increase elapsed time
            {
                //Pop off top element, increase time, push back to stack
                HandPoseData data = handPoseDataStack[0];
                data.elapsedTime += Time.deltaTime;
                handPoseDataStack[0] = data;
            }
        }
    }

    /// <summary>
    /// Used in UpdateStack, adds a new HandPoseData struct with the current time values and pose
    /// </summary>
    void AddNewStackElement()
    {
        //Add new pose data struct
        HandPoseData poseData = new HandPoseData();

        poseData.pose = currentPose;
        poseData.elapsedTime = Time.deltaTime;
        poseData.startTime = Time.realtimeSinceStartup;
        poseData.timeBetweenPoses = 0;

        handPoseDataStack.Insert(0, poseData);
    }

    /// <summary>
    /// Logic for checking most recent poses against the known list of gestures
    /// </summary>
    /// <returns>Whether or not a gesture matched</returns>
    public bool CheckSingleHandGesture()
    {
        if (currentPose == null) { return false; }

        //NOT PERFORMANT, lots of loops as you add more poses
        //Maybe try a system that uses a temp list and goes through all wrist bones in each pose, remove any hand poses that don't match,
        //then continue to the next pose node but only for the HandPoses that matched at the wrist
        foreach (HandGesture gesture in handGestureList)
        {
            bool match = gesture.MatchGesture(handPoseDataStack);

            //All poses match
            if (match)
            {
                OnGestureEnter?.Invoke(gesture);
                handPoseDataStack.Clear();
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Display the hand target hand pose
    /// </summary>
    /// <param name="pose">Target pose to display</param>
    void DisplayHandPose(HandPose pose)
    {
        if (displayPose)
        {
            Destroy(displayHandPositionMarker.gameObject);
            Destroy(displayPose.gameObject);
        }

        currentDebugDisplayHandTime = debugDisplayHandTime;

        displayPose = Instantiate(pose);
        displayHandPositionMarker = Instantiate(handPositionMarkerPrefab);

        float toleranceRadius = displayPose.GetToleranceRadius();
        displayHandPositionMarker.transform.localScale = new Vector3(toleranceRadius, toleranceRadius, toleranceRadius);
    }

    /// <summary>
    /// Update position and rotation of the displayed hand pose
    /// </summary>
    public void UpdateDisplayHandPose()
    {
        if (debugDisplayHandPose && //If settings allow for display hand and display pose is valid, move it
            displayPose != null)
        {
            displayPose.transform.position = Camera.main.transform.position;
            displayPose.transform.rotation = Quaternion.Euler(
                displayPose.transform.rotation.eulerAngles.x,
                Camera.main.transform.eulerAngles.y,
                displayPose.transform.rotation.eulerAngles.z);

            Transform jointTransform = displayPose.GetJointTransform(0);
            displayHandPositionMarker.transform.position = jointTransform.position;
        }
        else if (displayPose)   //If display pose is valid, hide it
        {
            if (displayPose) { Destroy(displayPose.gameObject); }
            if (displayHandPositionMarker) { Destroy(displayHandPositionMarker.gameObject); }
        }

        //After a set time, destroy the display pose
        if(currentDebugDisplayHandTime >= 0)
        {
            currentDebugDisplayHandTime -= Time.deltaTime;

            if(currentDebugDisplayHandTime < 0)
            {
                if(displayPose) {  Destroy(displayPose.gameObject); }
                if(displayHandPositionMarker) { Destroy(displayHandPositionMarker.gameObject); }
            }
        }
    }

#if UNITY_EDITOR

    /// <summary>
    /// Save current hand pose to the passed HandPose, for use when calibrating signs
    /// </summary>
    /// <param name="pose"></param>
    void SaveHandPose(HandPose pose)
    {
        pose.SetHandPose(handCurrent);
    }

    /// <summary>
    /// Save current hand pose to prefab in currentEditorHandPose
    /// </summary>
    [ContextMenu("Save Hand Pose")]
    void EditorSaveHandPose()
    {
        if(modifyEditorHandPose)
        {
            currentEditorHandPose.SetHandPose(handCurrent);
        }
    }
#endif

    /// <summary>
    /// Search list of valid poses based on the display names in each pose
    /// </summary>
    /// <param name="displayName">Name to search for</param>
    /// <returns></returns>
    public HandPose GetHandPose(string displayName)
    {
        foreach (HandPose pose in handPoseList)
        {
            if (pose.GetDisplayName() == displayName) return pose;
        }

        return null;
    }

    /// <summary>
    /// Search list of valid gestures based on the display names in each gesture
    /// </summary>
    /// <param name="displayName">Name to search for</param>
    /// <returns></returns>
    public HandGesture GetHandGesture(string displayName)
    {
        foreach (HandGesture gesture in handGestureList)
        {
            if (gesture.GetDisplayName() == displayName) return gesture;
        }

        return null;
    }
}
