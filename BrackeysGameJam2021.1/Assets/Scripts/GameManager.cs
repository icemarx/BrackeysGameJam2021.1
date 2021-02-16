using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public Transform target = null;

    private List<Rigidbody2D> boids;
    public float max_speed = 1;
    public float max_distance = 1;
    public float steps = 100;     // used with cohesion, 100 means 1% towards the center of the group
    public float follow_weight = 0;
    public float cohesion_weight = 0;
    public float separation_weight = 0;
    public float alignment_weight = 0;

    void Start() {
        if(target == null) {
            target = GameObject.FindGameObjectWithTag("Player").transform;
        }

        // Set mouse cursor to not be visible
        Cursor.visible = false;

        // lock cursor to screen
        Cursor.lockState = CursorLockMode.Confined;

        // boids algorithm initialize
        boids = new List<Rigidbody2D>();
        foreach (GameObject go in GameObject.FindGameObjectsWithTag("Bird")) {
            boids.Add(go.GetComponent<Rigidbody2D>());
        }
    }

    private void Update() {
        // TODO: implement this without this many lists
        List<Vector2> positions = new List<Vector2>();
        List<Vector2> velocities = new List<Vector2>();

        // update positions, velocities
        foreach(Rigidbody2D b in boids) {
            // get neighbors
            List<Rigidbody2D> neighbors = new List<Rigidbody2D>();
            // TODO: replace this with space division
            foreach(Rigidbody2D n in boids) {
                if(b != n && Vector2.Distance(b.position, n.position) <= max_distance) {
                    neighbors.Add(n);
                }
            }

            Vector2 v1 = FollowTarget(b, new Vector2(target.position.x, target.position.y));
            Vector2 v2 = Cohesion(b, neighbors);
            Vector2 v3 = Separation(b, neighbors);
            Vector2 v4 = Alignment(b, neighbors);

            Vector2 v = b.velocity + v1 + v2 + v3 + v4;
            v = v.normalized * Mathf.Min(v.magnitude, max_speed);

            velocities.Add(v);
            positions.Add(b.position + v);

        }

        // apply to boid
        for(int i = 0; i < boids.Count; i++) {
            boids[i].position = positions[i];
            boids[i].velocity = velocities[i];
            // TODO: deal with rotation
        }
    }

    /// <summary>
    /// Returns the 2D velocity vector to steer the boid towards the target
    /// </summary>
    /// <param name="boid">Rigidbody2D of the boid</param>
    /// <param name="tar">Target position, towards which the boid should move</param>
    /// <returns>2D velocity vector that will steer the boid towards the target</returns>
    private Vector2 FollowTarget(Rigidbody2D boid, Vector2 tar) {
        return (tar-boid.position).normalized*follow_weight - boid.velocity;
    }

    /// <summary>
    /// Returns the 2D velocity vector that steers the boid towards cohesion,
    /// based on the center of the cluster of neighboring boids.
    /// </summary>
    /// <param name="boid">Rigidbody2D of the boid</param>
    /// <param name="neighbors">Rigidbody2Ds of other boids close enough to boid
    /// to consider relevant</param>
    /// <returns>cohesion velocity vector</returns>
    private Vector2 Cohesion(Rigidbody2D boid, List<Rigidbody2D> neighbors) {
        if (neighbors.Count <= 0) return Vector2.zero;

        // use neighbors
        Vector2 average_postion = Vector2.zero;
        foreach (Rigidbody2D n in neighbors) {
            average_postion += n.position;
        }
        average_postion /= neighbors.Count;

        return (average_postion - boid.position) * cohesion_weight / steps;
    }

    /// <summary>
    /// Returns the 2D velocity vector, that steers the boid away from neighboring boids
    /// in order to prevent collision.
    /// </summary>
    /// <param name="boid">Rigidbody2D of the boid</param>
    /// <param name="neighbors">Rigidbody2Ds of other boids close enough to boid
    /// to consider relevant</param>
    /// <returns>separation velocity vector</returns>
    private Vector2 Separation(Rigidbody2D boid, List<Rigidbody2D> neighbors) {
        Vector2 separation_velocity = Vector2.zero;
        foreach (Rigidbody2D n in neighbors) {
            float direction = Vector2.Distance(boid.position, n.position);
            separation_velocity += (boid.position - n.position)/ (direction*direction);
        }

        return separation_velocity.normalized*separation_weight;
    }

    /// <summary>
    /// Returns the 2D velocity vector, that will align the boids velocity with those
    /// of its neighbours. If there are no neighbors, return zero vector
    /// </summary>
    /// <param name="boid">Rigidbody2D of the boid</param>
    /// <param name="neighbors">Rigidbody2Ds of other boids close enough to boid
    /// to consider relevant</param>
    /// <returns>alignment velocity vector</returns>
    private Vector2 Alignment(Rigidbody2D boid, List<Rigidbody2D> neighbors) {
        if (neighbors.Count <= 0) return Vector2.zero;

        // use neighbors
        Vector2 average_velocity = Vector2.zero;
        foreach (Rigidbody2D n in neighbors) {
            average_velocity += n.velocity;
        }
        average_velocity /= neighbors.Count;

        return (average_velocity - boid.position).normalized * alignment_weight;
    }


}
