using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowScript : MonoBehaviour
{
    public float speed = 1;
    public float speed_mod = 0.01f;

    private void LateUpdate() {
        // get cursor position on screen
        Vector3 cursor_position = Camera.main.ScreenPointToRay(Input.mousePosition).origin;
        // Debug.Log(cursor_position);

        // get direction vector and magnitude
        Vector3 direction = cursor_position - transform.position;
        float magnitude = direction.magnitude;
        direction = direction.normalized;

        // move towards cursor
        float mod = magnitude * speed * speed_mod;
        // transform.position += direction * magnitude*magnitude * speed * speed_mod + direction * magnitude * speed * speed_mod;
        transform.position += direction * (mod*mod + mod);
    }
}
