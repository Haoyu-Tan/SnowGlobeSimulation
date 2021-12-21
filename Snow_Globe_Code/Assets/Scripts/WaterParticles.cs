using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaterParticles : MonoBehaviour
{
    // Start is called before the first frame update
    public Vector3 previousPos;
    public Vector3 velocity;
    //public int currentCube;
    //public List<int> myNeighborCube;
    //Vector3 currentPos;

    // When the particle first initialized 
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    /*
    public void UpdateCube()
    {
        currentCube = pointLocation(this.transform.localPosition);
    }




    // Using the rad to check which cube it will intersect
    public void UpdateNeighborCube(float rad)
    {
        List<int> result = new List<int>();
        result.Add(currentCube);
        int output = -1;
        //
        Vector3 currentVec = this.transform.localPosition + rad * new Vector3(1,1,1);
        output = pointLocation(currentVec);
        if (!result.Contains(output))
            result.Add(output);
        //
        currentVec = this.transform.localPosition + rad * new Vector3(1, 1, -1);
        output = pointLocation(currentVec);
        if (!result.Contains(output))
            result.Add(output);
        //
        currentVec = this.transform.localPosition + rad * new Vector3(1, -1, 1);
        output = pointLocation(currentVec);
        if (!result.Contains(output))
            result.Add(output);
        //
        currentVec = this.transform.localPosition + rad * new Vector3(1, -1, -1);
        output = pointLocation(currentVec);
        if (!result.Contains(output))
            result.Add(output);
        //
        currentVec = this.transform.localPosition + rad * new Vector3(-1, 1, 1);
        output = pointLocation(currentVec);
        if (!result.Contains(output))
            result.Add(output);
        //
        currentVec = this.transform.localPosition + rad * new Vector3(-1, 1, -1);
        output = pointLocation(currentVec);
        if (!result.Contains(output))
            result.Add(output);
        //
        currentVec = this.transform.localPosition + rad * new Vector3(-1, -1, 1);
        output = pointLocation(currentVec);
        if (!result.Contains(output))
            result.Add(output);
        //
        currentVec = this.transform.localPosition + rad * new Vector3(-1, -1, -1);
        output = pointLocation(currentVec);
        if (!result.Contains(output))
            result.Add(output);
        myNeighborCube = result;
    }



    public int pointLocation(Vector3 vec)
    {
        float yBound = 0.2f;
        if (vec.x > 0)
        {
            if (vec.y > yBound)
            {
                if (vec.z > 0)
                {
                    return 1;
                }
                else
                {
                    return 2;
                }
            }
            else
            {
                if (vec.z > 0)
                {
                    return 3;
                }
                else
                {
                    return 4;
                }
            }
        }
        else
        {
            if (vec.y > yBound)
            {
                if (vec.z > 0)
                {
                    return 5;
                }
                else
                {
                    return 6;
                }
            }
            else
            {
                if (vec.z > 0)
                {
                    return 7;
                }
                else
                {
                    return 8;
                }
            }
        }
    }
    */
}
