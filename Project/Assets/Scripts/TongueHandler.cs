using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TongueHandler : MonoBehaviour
{
    //  Vector3 target;
    // Start is called before the first frame update
    void Start()
    {
        // target = this.parent.GetComponentInChildren<PlayerNetwork>().hitTarget;
    }

    // Update is called once per frame
    void Update()
    {

         //  this.position = Vector3.Lerp(this.parent.transform.position, target, 0.5f);
        //  this.localScale = new Vector3(0.1f, Vector3.Distance(this.parent.transform.position, target), 0.1f);
         //  this.LookAt(target);
    }
    public void updateTongue(Vector3 target, Vector3 Owner)
    {
        Debug.Log(Owner + "PLAYER");
        Debug.Log(target + "HITTARGET");
        Vector3 start = new Vector3(Owner.x, Owner.y + 1.1f, Owner.z);
        this.transform.position = Vector3.Lerp(start, target, 0.5f);
         this.transform.localScale = new Vector3(0.1f, Vector3.Distance(start, target), 0.1f);
         this.transform.LookAt(start);
        this.transform.Rotate(90,0,0);
    }
}
