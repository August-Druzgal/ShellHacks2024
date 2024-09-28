using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct GestureData
{
    public Gesture gesture;     //Gesture in question
    public float elapsedTime;   //Time before next gesture was made
}

public class SpellCastComponent : MonoBehaviour
{
    TwoHandPoseTracker twoHandPoseTracker;

    [SerializeField] List<Spell> spells;

    //Most recent signs inserted at index 0
    public List<GestureData> recentHandGestures = new List<GestureData>();

    //Seconds to wait before allowing the same sign again
    [SerializeField] float sameSignDelay = 1f;
    float currentDelay = 0f;

    //Max time between gestures before they are rejected as part of the same spell
    [SerializeField] float maxTimeBetweenGestures = 1f;

    [SerializeField] Transform spellSpawnPoint;

    private void Start()
    {
        for(int i = 0; i < spells.Count; i++)
        {
            //Set and reverse gestures list
            Spell spell = spells[i];
            spell.reversedGestures = new(spell.gestures);
            spell.reversedGestures.Reverse();
            spells[i] = spell;
        }
    }

    private void OnEnable()
    {
        twoHandPoseTracker = GetComponent<TwoHandPoseTracker>();
        twoHandPoseTracker.OnGestureEnter.AddListener(AddRecentGesture);
        twoHandPoseTracker.leftTracker.OnGestureEnter.AddListener(AddRecentGesture);
        twoHandPoseTracker.rightTracker.OnGestureEnter.AddListener(AddRecentGesture);
        
    }

    private void OnDisable()
    {
        twoHandPoseTracker.OnGestureEnter.RemoveListener(AddRecentGesture);
        twoHandPoseTracker.leftTracker.OnGestureEnter.RemoveListener(AddRecentGesture);
        twoHandPoseTracker.rightTracker.OnGestureEnter.RemoveListener(AddRecentGesture);
    }

    private void Update()
    {
        //Update delay timer
        if(currentDelay > 0f)
        {
            currentDelay -= Time.deltaTime;
        }

        //Incremenet elapsed time on most recent hand gesture
        if(recentHandGestures.Count > 0)
        {
            GestureData data = recentHandGestures[0];
            data.elapsedTime += Time.deltaTime;
            recentHandGestures[0] = data;
        }
    }

    void AddRecentGesture(Gesture handGesture)
    {
        if(recentHandGestures.Count > 0 &&          //Check there is a count
            recentHandGestures[0].gesture == handGesture && //if current gesture equals the one on top
            currentDelay > 0)                       //and currentDelay has not elapsed
        {
            //Then do nothing
            return;
        }

        //Otherwise we have a new gesture
        GestureData newData = new GestureData();
        newData.gesture = handGesture;
        newData.elapsedTime = Time.deltaTime;

        //Add gesture to list
        recentHandGestures.Insert(0, newData);
        Debug.Log($"Add gesture - {recentHandGestures.Count}");

        currentDelay = sameSignDelay;

        //Iterate over all spells
        foreach (Spell spellEffect in spells)
        {
            //Check if there are less letters than gestuers in spell effect
            if (recentHandGestures.Count < spellEffect.reversedGestures.Count)
            {
                //Skip this gesture
                continue;
            }

            //Check order of poses, ensure they match the most recent poses in the stack
            bool match = true;
            for (int i = 0; i < spellEffect.reversedGestures.Count; i++)
            {
                //Exit loop if a pose does not match, move to next pose or exit and accept if at end of pose list
                if (spellEffect.reversedGestures[i] != recentHandGestures[i].gesture ||
                    recentHandGestures[i].elapsedTime > maxTimeBetweenGestures)
                {
                    match = false;
                    break;
                }
            }

            //All poses match
            if (match)
            {
                spellEffect.effect.CastSpell(spellSpawnPoint);
                recentHandGestures.Clear();
            }
        }
    }
}
