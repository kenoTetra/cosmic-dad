using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFollowObject : MonoBehaviour
{
    // Vars
    public GameObject player;


    // Update is called once per frame
    void Update()
    {
        transform.position = new Vector3(0f, player.transform.position.y, -10f);
    }
}
