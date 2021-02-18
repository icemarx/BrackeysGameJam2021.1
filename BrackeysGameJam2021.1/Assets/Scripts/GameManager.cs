using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // game settings
    public bool spawn_active = false;
    public int spawn_num = 1;

    // random field
    private float rand_min_x = -8.5f;
    private float rand_max_x = 8.5f;
    private float rand_min_y = -3.5f;
    private float rand_max_y = 4.5f;

    public Transform leader = null;     // target transform
    public GameObject bird = null;      // bird object instance (for spawning)
    public GameObject egg = null;       // egg object instance (for spawning)

    // bird details
    public float max_follow_speed = 1;
    public float max_avoid_speed = 1;

    // boids algorithm attributes
    private static List<Rigidbody2D> boids = new List<Rigidbody2D>();
    public float follow_threshold = 0.5f;
    public float avoid_threshold = 1;
    public int max_neighbors_num = 20;
    // public float max_speed = 1;  // depricated
    public float max_distance = 1;
    public float steps = 100;     // used with cohesion, 100 means 1% towards the center of the group
    public float follow_weight = 0;
    public float steering_weight = 0;
    public float cohesion_weight = 0;
    public float separation_weight = 0;
    public float alignment_weight = 0;

    // game statistics
    private static int num_of_birds = 0;

    void Start() {
        if(leader == null) {
            leader = GameObject.FindGameObjectWithTag("Player").transform;
        }

        // Set mouse cursor to not be visible
        Cursor.visible = false;

        // lock cursor to screen
        Cursor.lockState = CursorLockMode.Confined;

        // spawn the first egg
        SpawnEgg();
    }

    private void Update() {
        // check for spawn button
        if(spawn_active && Input.GetKeyDown(KeyCode.S) && bird != null) {
            // spawn a bird at (0,i)
            Debug.Log("SPAWN BIRD");
            for (int i = 0; i < spawn_num; i++) {
                Instantiate(bird, Vector2.up * i, Quaternion.identity);
            }
        } else if(spawn_active && Input.GetKeyDown(KeyCode.E) && egg != null) {
            // spawn an egg at (0,0)
            Debug.Log("SPAWN EGG");
            Instantiate(egg, Vector2.zero, Quaternion.identity);
        } else if(spawn_active && Input.GetKeyDown(KeyCode.D) && egg != null) {
            Debug.Log("SPAWN EGGS");
            for (int i = 0; i < spawn_num; i++) {
                // TODO: get random vector in visible range
                SpawnEgg();
            }
        }

        /*
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
        */
    }

    /// <summary>
    /// Called by birds, it notifies the GameManager that a new bird has been spawned
    /// and should be added to the list of all birds.
    /// </summary>
    /// <param name="bird">The newly created bird</param>
    public static void ImHere(GameObject bird_go) {
        boids.Add(bird_go.GetComponent<Rigidbody2D>());

        num_of_birds++;
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
    /// Spawns an egg GameObject onto a random point in the sceene, determined by the
    /// preset limits.
    /// </summary>
    public void SpawnEgg() {
        float x = Random.value * (rand_max_x - rand_min_x) + rand_min_x;
        float y = Random.value * (rand_max_y - rand_min_y) + rand_min_y;
        Instantiate(egg, new Vector2(x, y), Quaternion.identity);
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

        return (average_velocity - boid.velocity).normalized * alignment_weight;
    }


}
