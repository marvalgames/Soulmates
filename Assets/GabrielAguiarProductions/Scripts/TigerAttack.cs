using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TigerAttack : MonoBehaviour
{
    public bool followGround = true;
    public float speed = 30;
    public float slowDownRate = 0.01f;
    public float detectingDistance = 0.1f;
    public float destroyDelay = 5;
    public float objectsToDetachDelay = 2;
    public List<GameObject> objectsToDetach = new List<GameObject>();
    [Space]
    public float erodeInRate = 0.06f;
    public float erodeOutRate = 0.03f;
    public float erodeRefreshRate = 0.01f;
    public float erodeAwayDelay = 1.25f;
    public List<SkinnedMeshRenderer> objectsToErode = new List<SkinnedMeshRenderer>();

    private Rigidbody rb;
    private bool stopped;

    void Start()
    {
        if(followGround)
            transform.position = new Vector3(transform.position.x, 0, transform.position.z);

        if (GetComponent<Rigidbody>() != null)
        {
            rb = GetComponent<Rigidbody>();
            StartCoroutine(SlowDown());
        }
        else
            Debug.Log("No Rigidbody");

        if (objectsToDetach != null)
            StartCoroutine(DetachObjects());

        if (objectsToErode != null)
            StartCoroutine(ErodeObjects());

        Destroy(gameObject, destroyDelay);
    }

    private void FixedUpdate()
    {
        if (!stopped && followGround)
        {
            RaycastHit hit;
            Vector3 distance = new Vector3(transform.position.x, transform.position.y + 1, transform.position.z);
            if (Physics.Raycast(distance, transform.TransformDirection(-Vector3.up), out hit, detectingDistance))
            {
                transform.position = new Vector3(transform.position.x, hit.point.y, transform.position.z);
            }
            else
            {
                transform.position = new Vector3(transform.position.x, 0, transform.position.z);
            }
            Debug.DrawRay(distance, transform.TransformDirection(-Vector3.up * detectingDistance), Color.red);
        }
    }

    IEnumerator SlowDown ()
    {
        float t = 1;
        while (t > 0)
        {
            rb.linearVelocity = Vector3.Lerp(Vector3.zero, rb.linearVelocity, t);
            t -= slowDownRate;
            yield return new WaitForSeconds(0.1f);
        }

        stopped = true;
    }

    IEnumerator DetachObjects ()
    {
        yield return new WaitForSeconds(objectsToDetachDelay);

        for (int i=0; i<objectsToDetach.Count; i++)
        {
            objectsToDetach[i].transform.parent = null;
            Destroy(objectsToDetach[i], objectsToDetachDelay);
        }
    }

    IEnumerator ErodeObjects()
    {
        for (int i = 0; i < objectsToErode.Count; i++)
        {
            float t = 1;
            while (t > 0)
            {
                t -= erodeInRate;
                for (int j = 0; j < objectsToErode[i].materials.Length; j++)
                {
                    objectsToErode[i].materials[j].SetFloat("_Erode", t);
                }
                yield return new WaitForSeconds(erodeRefreshRate);
            }
        }

        yield return new WaitForSeconds(erodeAwayDelay);

        for (int i = 0; i < objectsToErode.Count; i++)
        {
            float t = 0;
            while (t < 1)
            {
                t += erodeOutRate;
                for (int j = 0; j < objectsToErode[i].materials.Length; j++)
                {
                    objectsToErode[i].materials[j].SetFloat("_Erode", t);
                }
                yield return new WaitForSeconds(erodeRefreshRate);
            }
        }
    }
}
