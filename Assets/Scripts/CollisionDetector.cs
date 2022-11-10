using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionDetector : MonoBehaviour
{
    public GameObject collidedObject;

    public bool collided;

    // Start is called before the first frame update
    void Start()
    {
    }

    // Update is called once per frame
    void Update()
    {
    }

    void OnCollisionEnter(Collision other)
    {
        collided = true;
        collidedObject = other.gameObject;
    }

    void OnCollisionExit()
    {
        collided = false;
        collidedObject = null;
    }
}
