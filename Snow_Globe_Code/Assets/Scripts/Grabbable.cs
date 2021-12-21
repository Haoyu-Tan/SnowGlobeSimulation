using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grabbable : MonoBehaviour
{
    // Start is called before the first frame update
    [SerializeField]
    private GameObject indicator;
    public Vector3 deltaV;
    private Vector3 center;
    private float yVal;
    private float validRad;
    void Start()
    {
        deltaV = Vector3.zero;
        center = this.transform.position;
        yVal = center.y;
        center.y = 0;
        validRad = 2f;
    }

    public void ChangePosition(Vector3 vnew)
    {
        Vector3 newPos = vnew;
        //deltaV = newPos - this.transform.position;
        //this.transform.position = newPos;
        //return;
        newPos.y = 0;
        Vector3 dir = (newPos - center);
        float mag = dir.magnitude;
        if (mag > validRad)
        {
            newPos = dir / mag * validRad + center + Vector3.up * yVal;

        }
        else
        {
            newPos = dir + center + Vector3.up * yVal;
        }
        deltaV = newPos - this.transform.position;
        this.transform.position = newPos;


    }

    public void NoUpdate()
    {
        deltaV = Vector3.zero;
    }

    public void Release()
    {
        deltaV = Vector3.zero;
        EnableIndicator();
        this.GetComponent<BoxCollider>().enabled = true;
    }


    public void Grab()
    {
        DisableIndicator();
        this.GetComponent<BoxCollider>().enabled = false;
    }

    public void EnableIndicator()
    {
        indicator.SetActive(true);
    }

    public void DisableIndicator()
    {
        indicator.SetActive(false);
    }


    // Update is called once per frame
    void Update()
    {
        
    }
}
