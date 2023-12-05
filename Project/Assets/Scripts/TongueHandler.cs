using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class TongueHandler : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {

    }

    //method that gets the connection points for a tongue, (the player and the hit point)
    //makes position, rotation, and scale of the tongue become a stretched capsule connecting the player and target.
    public void updateTongue(Vector3 target, Vector3 Owner)
    {
        Vector3 start = new Vector3(Owner.x, Owner.y + 1.1f, Owner.z);
        this.transform.position = Vector3.Lerp(start, target, 0.5f);
         this.transform.localScale = new Vector3(0.1f, Vector3.Distance(start, target), 0.1f);
         this.transform.LookAt(start);
        this.transform.Rotate(90,0,0);
    }
}
