using UnityEngine;

public class FollowTarget : MonoBehaviour
{
    public Transform target;
    Vector3 positionOffset;
    Quaternion rotationOffset = Quaternion.identity;
    
    public bool followOnStart = false;
    public bool offsetPosOnStart = false;
    public bool offsetRotOnStart = false;


    public bool followPosition = true;
    public bool instantFollowPos = false;
    public float positionTime = .5f;
    public bool followX = true;
    public bool followY = true;
    public bool followZ = true;


    public bool followRotation = true;
    public bool instantFollowRot = false;
    public float rotationTime = .5f;
    public bool followXrot = true;
    public bool followYrot = true;
    public bool followZrot = true;

    public bool lookAtTarget = false;


    private bool isFollowing = false;
    private Vector3 velocity = Vector3.zero;
    private Quaternion velQuat = Quaternion.identity;

    public virtual void Start()
    {
        if (followOnStart)
        {
            if (offsetPosOnStart)
            {
                _StartFollowWithOffset();
            }
            else
            {
                _StartFollow();
            }

        }
    }

    void FixedUpdate()
    {
        if (isFollowing && target)
        {
            if(!instantFollowPos)
            {
                transform.position = PositionSolver();
            }

            transform.rotation = RotationSolver();
        }
    }

    void Update()
    {
        if (isFollowing && target)
        {
            if (instantFollowPos)
            {
                transform.position = PositionSolver();
            }
        }
    }

    public virtual Vector3 PositionSolver()
    {
        if (!followPosition)
        {
            return transform.position;
        }

        Vector3 targetPosition = target.position + positionOffset;

        // Apply the followX, followY, followZ constraints
        if (!followX) targetPosition.x = transform.position.x;
        if (!followY) targetPosition.y = transform.position.y;
        if (!followZ) targetPosition.z = transform.position.z;

        if (instantFollowPos)
        {
            return targetPosition;
        }
        else
        {
            return Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, positionTime);
        }
    }

    public virtual Quaternion RotationSolver()
    {
        Quaternion result;
        if (!followRotation)
        {
            return transform.rotation;
        }
        if (lookAtTarget)
        {
            Vector3 direction = target.position - transform.position;
            result = Quaternion.LookRotation(direction);
        }
        else
        {
            if(rotationOffset == Quaternion.identity)
            {
                result = target.rotation;
            }
            else
            {
                result = target.rotation * rotationOffset;
            }

        }
        // Apply the followX, followY, followZ constraints
        Vector3 eulerResult = result.eulerAngles;
        Vector3 currentEuler = transform.rotation.eulerAngles;

        result = Quaternion.Euler(
            followXrot ? eulerResult.x : currentEuler.x,
            followYrot ? eulerResult.y : currentEuler.y,
            followZrot ? eulerResult.z : currentEuler.z
        );

        if (instantFollowRot)
        {
            return result;
        }
        else
        {
            return QuaternionExtensions.SmoothDamp(transform.rotation, result, ref velQuat, rotationTime, Time.deltaTime);
        }
    }

    public void _ChangeTarget(Transform myTarget)
    {
        target = myTarget;
    }

    public void _StartFollow()
    {
        isFollowing = true;
    }
    public void _StopFollow()
    {
        isFollowing = false;
    }
    public void _StartFollowWithOffset()
    {
        positionOffset = transform.position - target.position;
        if(offsetRotOnStart)
        {
            rotationOffset = Quaternion.Inverse(target.rotation) * transform.rotation;
        }
        else
        {
            rotationOffset = Quaternion.identity;
        }
        _StartFollow();
    }
}

public static class QuaternionExtensions
{
    public static Quaternion SmoothDamp(Quaternion current, Quaternion target, ref Quaternion velocity, float smoothTime, float deltaTime)
    {
        // Calculate the difference between the current and target rotations
        float dot = Quaternion.Dot(current, target);
        float multi = dot > 0.0f ? 1.0f : -1.0f;
        target.x *= multi;
        target.y *= multi;
        target.z *= multi;
        target.w *= multi;

        // Smoothly interpolate the rotation
        Vector4 result = new Vector4(
            Mathf.SmoothDamp(current.x, target.x, ref velocity.x, smoothTime, Mathf.Infinity, deltaTime),
            Mathf.SmoothDamp(current.y, target.y, ref velocity.y, smoothTime, Mathf.Infinity, deltaTime),
            Mathf.SmoothDamp(current.z, target.z, ref velocity.z, smoothTime, Mathf.Infinity, deltaTime),
            Mathf.SmoothDamp(current.w, target.w, ref velocity.w, smoothTime, Mathf.Infinity, deltaTime)
        ).normalized;

        return new Quaternion(result.x, result.y, result.z, result.w);
    }
}