using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioHolder : MonoBehaviour
{
    public AudioClip[] audioClips;
    public AudioSource audioSource1;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void playAudio(int index)
    {
        audioSource1.clip = audioClips[index];
        audioSource1.Play();
    }
}
