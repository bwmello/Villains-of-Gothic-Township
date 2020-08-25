using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Explosion : MonoBehaviour
{
    public bool isLooping = false;


    void Start()
    {
        if (!isLooping)
        {
            Destroy(gameObject, transform.GetComponentInChildren<Animator>().GetCurrentAnimatorStateInfo(0).length);
        }
    }
}
