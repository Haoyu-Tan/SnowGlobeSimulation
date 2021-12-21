using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Lifter : MonoBehaviour
{
    // Start is called before the first frame update
    public Vector3 myFinalVelocity;
    public float acc = 5;
    public float maxVel = 30;
    private int currMode = 0;
    private int maxMode = 4;
    private float vAngle = 10;
    private int ind = -1;
    private Vector3 currAngle;
    void Start()
    {
        myFinalVelocity = this.transform.up * maxVel;
        this.GetComponent<BoxCollider>().enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if(currMode == 3)
        {
            currAngle.x += vAngle * Time.deltaTime * ind;
            if (currAngle.x > 60f)
            {
                currAngle.x = 60f;
                ind = -1;
            }
            if (currAngle.x < 20f)
            {
                currAngle.x = 20f;
                ind = 1;
            }

            this.transform.localRotation = Quaternion.Euler(currAngle);

            myFinalVelocity = this.transform.up * maxVel;
        }

    }

    public void ChangeMode()
    {
        currMode += 1;
        if (currMode >= maxMode) currMode -= maxMode;
        switch (currMode)
        {
            case 0:
                this.GetComponent<BoxCollider>().enabled = false;
                break;
            case 1:
                this.GetComponent<BoxCollider>().enabled = true;
                maxVel = 5f;
                this.transform.localRotation = Quaternion.Euler(0, 0, 0);
                //this.transform.localPosition = new Vector3(0, -0.3f, 0);
                myFinalVelocity = this.transform.up * maxVel;
                break;
            case 2:
                this.transform.localRotation = Quaternion.Euler(30, 0, 0);

                myFinalVelocity = this.transform.up * maxVel;
                break;
            case 3:
                this.transform.localRotation = Quaternion.Euler(30, 0, 0);
                currAngle = new Vector3(30, 0, 0);
                //this.transform.localPosition = new Vector3(0, 0, 0);
                maxVel = 6f;
                ind = -1;
                myFinalVelocity = this.transform.up * maxVel;
                break;

        }
    }
}
