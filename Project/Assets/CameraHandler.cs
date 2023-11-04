using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraHandler : MonoBehaviour
{
    public Transform targetTransform;
    public Transform cameraTransform;
    public Transform cameraPivotTransform;
    private Transform thisTransform;
    private Vector3 cameraTransformPosition;
    public static CameraHandler singleton;
    public float lookSpeed = 0.1f;
    public float followSpeed = 0.1f;
    public float pivotSpeed = 0.03f;
    private float defaultPos;
    private float lookAngle;
    private float pivotAngle;
    public float minimumPivot = -35;
    public float maximumPivot = 35;
    public bool inputReceived = false;



    private void Awake()
    {
        singleton = this;
        thisTransform = transform;
        defaultPos = cameraTransform.localPosition.z;
    }
   

       //lerp the cameraposition to the playerposition
    public void FollowTarget(float d)
    {
        Vector3 targetPosition = Vector3.Lerp(thisTransform.position, targetTransform.position, d / followSpeed);
        thisTransform.position = targetPosition;
    }
    //two child gameobjects, one controls yaw, one controls pitch
    public void CamRotation(float d, float mouseXInput, float mouseYInput)
    {
        //yaw is based on x input
        //pitch is based on y input
        lookAngle += (mouseXInput * lookSpeed) / d;
        pivotAngle -= (mouseYInput * pivotSpeed) / d;

        //cap the pitch so u can't do a 360 vertically
        pivotAngle = Mathf.Clamp(pivotAngle, minimumPivot, maximumPivot);

        //set camera's rotation to the yaw
        Vector3 rotation = Vector3.zero;
        rotation.y = lookAngle;
        Quaternion targetRotation = Quaternion.Euler(rotation);
        thisTransform.rotation = targetRotation;

        //set camera pivot to the pitch
        rotation = Vector3.zero;
        rotation.x = pivotAngle;
        targetRotation = Quaternion.Euler(rotation);
        cameraPivotTransform.localRotation = targetRotation;
    }
}
