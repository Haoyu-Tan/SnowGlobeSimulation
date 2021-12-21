
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mouse : MonoBehaviour
{
    public Vector3 screenSpace;
    public Vector3 offset;
    private Transform currObject;
    private Transform grabbedObject;
    [SerializeField]
    private BoxCollider tableSurface;
    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
        if (!grabbedObject)
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            int layerMask = 1 << 9;
            if(Physics.Raycast(ray, out hit, 100, layerMask))
            {
                if (hit.collider.GetComponent<Grabbable>())
                {
                    if (currObject && currObject.name != hit.collider.transform.name)
                    {
                        DisableIndicator();   
                    }
                    currObject = hit.collider.transform;



                    if (Input.GetMouseButtonDown(0))
                    {

                        grabbedObject = currObject;
                        grabbedObject.GetComponent<Grabbable>().Grab();
                        tableSurface.enabled = true;

                    }
                    else
                    {
                        EnableIndicator();
                    }

                }
                else if (hit.collider.GetComponent<Button>())
                {
                    if (currObject && currObject.name != hit.collider.transform.name)
                    {
                        DisableIndicator();
                    }
                    currObject = hit.collider.transform;
                    if (Input.GetMouseButtonDown(0))
                    {
                        currObject.GetComponent<Button>().ButtonDown();
                    }
                    EnableIndicator();
                }
                else
                {
                    DisableIndicator();
                }
            }
            else if (currObject)
            {
                DisableIndicator();
                currObject = null;
            }

        }
        if (grabbedObject)
        {
            if (Input.GetMouseButtonUp(0))
            {

                // release
                grabbedObject.GetComponent<Grabbable>().Release() ;
                grabbedObject = null;
                tableSurface.enabled = false;

            }
            else
            {
                // move
                RaycastHit hit;
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                int layerMask = 1 << 9;
                if(Physics.Raycast(ray, out hit, 100, layerMask))
                {
                    grabbedObject.GetComponent<Grabbable>().ChangePosition(hit.point);

                }
                else
                {
                    grabbedObject.GetComponent<Grabbable>().NoUpdate();
                }
            }
        }
    }

    private void DisableIndicator()
    {
        if (!currObject) return;
        if (currObject.GetComponent<Grabbable>())
        {
            currObject.GetComponent<Grabbable>().DisableIndicator();
        }
        else if (currObject.GetComponent<Button>())
        {
            currObject.GetComponent<Button>().DisableIndicator();
        }
    }

    private void EnableIndicator()
    {
        if (!currObject) return;
        if (currObject.GetComponent<Grabbable>())
        {
            currObject.GetComponent<Grabbable>().EnableIndicator();
        }
        else if (currObject.GetComponent<Button>())
        {
            currObject.GetComponent<Button>().EnableIndicator();
        }
    }


}
