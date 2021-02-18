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
    private float avoid_threshold = 1;

    private const int FOLLOW = 1;
    private const int AVOID = 0;
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

        avoid_threshold = gm.avoid_threshold + Random.Range(-0.1f, 0.1f);

        neighbors = new Collider2D[gm.max_neighbors_num];
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
    private Collider2D[] neighbors;
    private void LateUpdate() {
        // get neighbors
        int num_neighbors = Physics2D.OverlapCircleNonAlloc(transform.position, gm.max_distance, neighbors);
        // Debug.Log(num_neighbors);

        // check distance towards cursor
        Vector2 desired_direction = gm.leader.position - transform.position;
        
        // determine steering and cap speed
        Vector2 steering = status * desired_direction.normalized * gm.follow_weight + rb.velocity;
        steering = steering.normalized * Mathf.Min(steering.magnitude, max_follow_speed);


        // check for status changes
        if (status == FOLLOW && desired_direction.magnitude < gm.follow_threshold) {
            status = AVOID;
            // offset = Random.insideUnitCircle.normalized * Mathf.Min(steering.magnitude, max_avoid_speed);
        } else if (status == AVOID && desired_direction.magnitude > avoid_threshold) {
            status = FOLLOW;
        }
        /*
        // fixed vector while avoidance
        if (status == AVOID) {
            steering = offset;
        } else */
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

                // average position
                average_position += neighbors[i].transform.position;

                // average velocity
                average_velocity += neighbors[i].attachedRigidbody.velocity;
            }
            separation_velocity *= gm.separation_weight;

            // calculate cohesion
            average_position /= num_neighbors;
            Vector2 cohesion_velocity = (Vector2) (average_position - transform.position) * gm.cohesion_weight / gm.steps;

            // calculate alignment
            average_velocity /= num_neighbors;
            Vector2 alignment = (average_velocity - rb.velocity).normalized * gm.alignment_weight;

            // deal with speed
            steering += separation_velocity + cohesion_velocity + alignment;           

        }
        steering = steering.normalized * Mathf.Min(steering.magnitude, max_follow_speed);
        // if (status == FOLLOW) steering = steering.normalized * Mathf.Min(steering.magnitude, max_follow_speed);
        // else steering = steering.normalized * Mathf.Min(steering.magnitude, max_avoid_speed);

        // compute and apply steering
        rb.velocity = steering;

    }

    /*
     * private Vector2 Alignment(Rigidbody2D boid, List<Rigidbody2D> neighbors) {
        if (neighbors.Count <= 0) return Vector2.zero;

        // use neighbors
        Vector2 average_velocity = Vector2.zero;
        foreach (Rigidbody2D n in neighbors) {
            average_velocity += n.velocity;
        }
        average_velocity /= neighbors.Count;

        return (average_velocity - boid.position).normalized * alignment_weight;
    }*/
}
