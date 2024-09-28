using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LazyFollow : MonoBehaviour
{
    public float followMin = 0.5f;
    public float stopMin = 0.05f;
    public float deltaTime = 0;
    public GameObject player;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        updatePos();
    }

    void updatePos()
    {
        deltaTime += Time.deltaTime;

        if (deltaTime >= 0.1f)
        {
            deltaTime -= 0.1f;
            Vector3 pos = this.transform.position;
            Vector3 target_pos = player.transform.position;
            Vector3 diff = pos - target_pos;

            if (diff.magnitude < stopMin)
            {
                GetComponent<FollowTarget>()._StopFollow();
            }
            else if (diff.magnitude > followMin)
            {
                GetComponent<FollowTarget>()._StartFollow();
            }
        }

    }
}
