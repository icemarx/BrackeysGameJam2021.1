using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowScript : MonoBehaviour
{
    public float speed = 1;
    public float speed_mod = 0.01f;

    public float max_distance = 1;
    
    private GameObject leader;

    private void Start() {
        leader = GameObject.FindGameObjectWithTag("Player");
        GameManager.ImHere(gameObject);
    }

    /*
    private void LateUpdate() {
        Vector3 movement_dir = leader.transform.position - transform.position;      // movement direction

        // get direction vector and magnitude
        Vector3 direction = movement_dir;

        float magnitude = direction.magnitude;

        // move towards cursor
        float mod = magnitude * speed * speed_mod;
        direction = direction.normalized * (mod * mod + mod);
        // Debug.Log(direction);
        
        // transform.position += direction * magnitude*magnitude * speed * speed_mod + direction * magnitude * speed * speed_mod;

        transform.position += direction;


    }
    */
}
