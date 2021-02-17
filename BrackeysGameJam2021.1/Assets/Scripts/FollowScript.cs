using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowScript : MonoBehaviour
{
    private GameManager gm;
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    public float max_follow_speed = 1;
    public float max_avoid_speed = 1;

    private const int FOLLOW = 0;
    private const int AVOID = 1;
    private int status = FOLLOW;


    [SerializeField]
    private Sprite upSprite;
    [SerializeField]
    private Sprite neutralSprite;
    [SerializeField]
    private Sprite downSprite;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    private void Start() {
        gm = FindObjectOfType<GameManager>();
        GameManager.ImHere(gameObject);

        max_follow_speed = gm.max_follow_speed + Random.Range(-1f, 1f);
        max_avoid_speed = gm.max_avoid_speed + Random.Range(-1f, 1f);
    }

    private void Update()
    {
        if (rb.velocity.y < -0.1f)
        {
            sr.sprite = downSprite;
        }
        else if (rb.velocity.y > 0.1f)
        {
            sr.sprite = upSprite;
        }
        else
        {
            sr.sprite = neutralSprite;
        }

        if (rb.velocity.x < 0f)
        {
            sr.flipX = false;
        }
        else if (rb.velocity.x > 0f)
        {
            sr.flipX = true;
        }
    }

    private Vector2 offset;
    private void LateUpdate() {
        // check distance towards cursor
        Vector2 desired_direction = gm.leader.position - transform.position;
       
        // determine steering and cap speed
        Vector2 steering = desired_direction.normalized * gm.follow_weight + rb.velocity;
        steering = steering.normalized * Mathf.Min(steering.magnitude, max_follow_speed);


        // check for status changes
        if (status == FOLLOW && desired_direction.magnitude < gm.follow_threshold) {
            status = AVOID;
            offset = Random.insideUnitCircle.normalized * Mathf.Min(steering.magnitude, max_avoid_speed);
        } else if (status == AVOID && desired_direction.magnitude > gm.avoid_threshold) {
            status = FOLLOW;
        }

        // fixed vector while avoidance
        if (status == AVOID) {
            steering = offset;
        }

        // compute and apply steering
        rb.velocity = steering;
    }

}
