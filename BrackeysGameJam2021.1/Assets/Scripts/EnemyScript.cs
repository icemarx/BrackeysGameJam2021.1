using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyScript : MonoBehaviour
{
    float min_y = -5;
    Vector3 target;
    bool go_left = true;
    public float speed = 1;
    public float speed_mod = 0.001f;

    private const int MOVING = 0;
    private const int JUMPING = 1;
    private const int FALLING = 2;
    int status = FALLING;

    void Start() {
        // TODO: get minimal y based on screen size
        min_y += 0.5f;   // half size of the monster
        target = Camera.main.ScreenPointToRay(Input.mousePosition).origin;
        go_left = target.x < transform.position.x;
    }

    private void LateUpdate() {
        Vector3 movement_direction = Vector3.zero;
        switch(status) {
            case MOVING:
                if (go_left) {
                    movement_direction = Vector3.left * speed * speed_mod;

                    // change status
                    if (target.x >= transform.position.x)
                        status = JUMPING;
                } else {
                    movement_direction = Vector3.right * speed * speed_mod;
                    
                    // change status
                    if (target.x < transform.position.x)
                        status = JUMPING;
                }

                break;

            case JUMPING:
                movement_direction = Vector3.up * speed * speed_mod;

                // change status
                if (transform.position.y >= Math.Max(target.y, min_y))
                    status = FALLING;

                break;

            case FALLING:
                movement_direction = Vector3.down * speed * speed_mod;

                // check status
                if (transform.position.y <= min_y) {
                    status = MOVING;

                    // change target
                    target = Camera.main.ScreenPointToRay(Input.mousePosition).origin;
                    go_left = target.x < transform.position.x;
                }

                break;
        }

        transform.position += movement_direction;
    }
}
