using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq.Expressions;
using Unity.VisualScripting;

public class Respawn : MonoBehaviour
{

    private Rigidbody rb;
    Vector3 startPostion;
    Vector3 currentPosition;


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        startPostion = new Vector3 (rb.position.x, rb.position.y, rb.position.z);
    }

    void FixedUpdate()
    {
        currentPosition = new Vector3 (rb.position.x, rb.position.y, rb.position.z);
        if (currentPosition.y < -5)
        {
            currentPosition = startPostion;
        }
    }

}