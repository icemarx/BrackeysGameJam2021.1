using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowScript : MonoBehaviour
{
    // components
    private GameManager gm;
    private Rigidbody2D rb;
    private SpriteRenderer sr;

    // speed
    private Collider2D[] neighbors;
    private float max_follow_speed = 1;
    private float max_avoid_speed = 1;
    private float avoid_threshold = 1;
    private float cohesion_weight = 0;

    // status
    private const int AVOID = 0;
    private const int FOLLOW = 1;
    private const int ATTACK = 2;
    private int status = FOLLOW;
    private int layer_mask;

    // sprites
    [Header("Sprites")]
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
        gm.ImHere(gameObject);

        max_follow_speed = gm.max_follow_speed + Random.Range(-1f, 1f);
        max_avoid_speed = gm.max_avoid_speed + Random.Range(-1f, 1f);
        avoid_threshold = gm.avoid_threshold + Random.Range(-0.1f, 0.1f);
        cohesion_weight = gm.cohesion_normal_weight;

        neighbors = new Collider2D[gm.max_neighbors_num];

        layer_mask = 1 << LayerMask.NameToLayer("BirdLayer");
    }

    private void Update()
    {
        // Checking speed to change crow sprite
        if (rb.velocity.y < -3f)
        {
            sr.sprite = downSprite;
        }
        else if (rb.velocity.y > 3f)
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
    
    private void FixedUpdate() {
        // get neighbors
        int num_neighbors = Physics2D.OverlapCircleNonAlloc(transform.position, gm.max_distance, neighbors, layer_mask);

        // check distance towards cursor
        Vector2 desired_direction = gm.leader.position - transform.position;
        
        // determine steering and cap speed
        Vector2 steering = status * desired_direction.normalized * gm.follow_weight + rb.velocity;
        steering = steering.normalized * Mathf.Min(steering.magnitude, max_follow_speed);


        // check for status changes
        if (Input.GetMouseButtonDown(0)) {
            status = ATTACK;
            cohesion_weight = gm.cohesion_attack_weight;
        } else if(Input.GetMouseButtonUp(0)) {
            status = FOLLOW; // follow or avoid will lead to checking for distance
            cohesion_weight = gm.cohesion_normal_weight;
        }

        if (status == FOLLOW && desired_direction.magnitude < gm.follow_threshold) status = AVOID;
        else if (status == AVOID && desired_direction.magnitude > avoid_threshold) status = FOLLOW;


        if(num_neighbors > 1) {
            // get the right vectors
            Vector2 separation_velocity = Vector2.zero;
            Vector3 average_position = Vector3.zero;
            Vector2 average_velocity = Vector2.zero;
            for (int i = 0; i < num_neighbors; i++) {
                // separation
                float separation_direction = Vector2.Distance(transform.position, neighbors[i].transform.position);
                if(separation_direction > 0.0001f)
                    separation_velocity += (Vector2)(transform.position - neighbors[i].transform.position) / (separation_direction * separation_direction);

                // averages
                average_position += neighbors[i].transform.position;
                average_velocity += neighbors[i].attachedRigidbody.velocity;
            }
            separation_velocity *= gm.separation_weight;

            // calculate cohesion, alignment and final steering
            average_position /= num_neighbors;
            Vector2 cohesion_velocity = (Vector2) (average_position - transform.position) * cohesion_weight / gm.steps;
            average_velocity /= num_neighbors;
            Vector2 alignment = (average_velocity - rb.velocity).normalized * gm.alignment_weight;

            steering += separation_velocity + cohesion_velocity + alignment;
        }


        // cap and apply steering
        if (status == FOLLOW) steering = steering.normalized * Mathf.Min(steering.magnitude, max_follow_speed);
        else steering = steering.normalized * Mathf.Min(steering.magnitude, max_avoid_speed);

        rb.velocity = steering;
    }
}
