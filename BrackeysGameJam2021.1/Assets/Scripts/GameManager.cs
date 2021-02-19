using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    // game settings
    public bool spawn_active = false;
    public int debug_spawn_num = 1;
    public bool zenMode = false;

    // game statistics
    public int max_bird_num = 50;
    private int num_of_birds = 0;
    public int numberOfBirds = 0;

    // random field
    private float rand_min_x = -8.5f;
    private float rand_max_x = 8.5f;
    private float rand_min_y = -3.5f;
    private float rand_max_y = 4.5f;

    public Transform leader = null;     // target transform
    public GameObject bird = null;      // bird object instance (for spawning)
    public GameObject egg = null;       // egg object instance (for spawning)
    public GameObject monster = null;   // monster objsect instance (for spawning)
    private bool egg_active = false; // true if is there at least one egg on the screen

    // zen mode
    [SerializeField]
    private int zen_spawn_num = 10;
    [SerializeField]
    private int zen_max_bird_num = 100;
    private bool mouse_leader = true;

    // bird details
    public float max_follow_speed = 1;
    public float max_avoid_speed = 1;

    // monster details
    [SerializeField]
    private float avg_spawn_time = 10;

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
    public float cohesion_normal_weight = 0;
    public float cohesion_attack_weight = 0;
    public float separation_weight = 0;
    public float alignment_weight = 0;

    // game score
    public int score = 0;
    [SerializeField]
    private int egg_hatch_score = 1;
    [SerializeField]
    private int bird_eaten_score = 0;
    [SerializeField]
    private int defeat_monster_score = 1;

    // UI references
    [SerializeField]
    private TextMeshProUGUI scoreText;
    [SerializeField]
    private TextMeshProUGUI birdCounter;
    [SerializeField]
    private SpriteRenderer cursorSprite;
    [SerializeField]
    private Sprite[] cursorSprites;
    private float MonsterToKillNumber = 10f;
    [SerializeField]
    private GameObject pauseScreen;
    [SerializeField]
    private GameObject gameOverScreen;

    private bool isGamePaused = false;

    void Start() {
        // Screen.SetResolution(1920, 1080, false);
        // QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;

        if (leader == null) {
            leader = GameObject.FindGameObjectWithTag("Player").transform;
        }

        // Set mouse cursor to not be visible
        Cursor.visible = false;

        // lock cursor to screen
        Cursor.lockState = CursorLockMode.Confined;

        if (zenMode) max_bird_num = zen_max_bird_num;

        // spawn the first egg
        SpawnEgg();

        // start monster spawn coroutine
        if(!zenMode) StartCoroutine("SpawnMonster");

        PauseGame(false);

    }

    private void Update() {
        numberOfBirds = num_of_birds;

        // update UI counters
        if(!zenMode) scoreText.text = string.Format("{0}", score);
        birdCounter.text = string.Format("{0} / {1}", num_of_birds, max_bird_num);

        // change cursor appearance based on number of birds and monster kill threshold
        int selectedCursorSprite = Mathf.RoundToInt(5f * num_of_birds / MonsterToKillNumber);
        selectedCursorSprite = Mathf.Min(selectedCursorSprite, 5);
        cursorSprite.sprite = cursorSprites[selectedCursorSprite];

        // pause game
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            isGamePaused = !isGamePaused;
            PauseGame(isGamePaused);
        }
        
        // check for game over
        if (num_of_birds <= 0f && !zenMode)
        {
            GameOver();
        }

        // check for spawn button
        if(spawn_active && !zenMode) {
            if (Input.GetKeyDown(KeyCode.S) && bird != null) {
                // spawn a bird at (0,i)
                Debug.Log("SPAWN BIRD");
                for (int i = 0; i < debug_spawn_num; i++) {
                    Instantiate(bird, Vector2.up * i, Quaternion.identity);
                }
            } else if (spawn_active && Input.GetKeyDown(KeyCode.E) && egg != null) {
                // spawn an egg at (0,0)
                Debug.Log("SPAWN EGG");
                Instantiate(egg, Vector2.zero, Quaternion.identity);
            } else if (spawn_active && Input.GetKeyDown(KeyCode.D) && egg != null) {
                Debug.Log("SPAWN EGGS");
                for (int i = 0; i < debug_spawn_num; i++) {
                    SpawnEgg();
                }
            }
        }

        if(zenMode) {
            // add birds
            if(Input.GetKeyDown(KeyCode.B) && num_of_birds < max_bird_num) {   // Spawn 1 bird
                Instantiate(bird, RandomPointInView(), Quaternion.identity);
            }
            if(Input.GetKeyDown(KeyCode.N)) {   // Spawn many birds
                for(int i = 0; i < zen_spawn_num && i + num_of_birds < max_bird_num; i++) {
                    Instantiate(bird, RandomPointInView(), Quaternion.identity);
                }
            }

            // remove birds
            if(Input.GetKeyDown(KeyCode.G)) {    // Remove 1 bird
                GameObject go = GameObject.FindGameObjectWithTag("Bird");
                if (go != null) EatBird(go);
            }
            if(Input.GetKeyDown(KeyCode.H)) {   // Remove many birds
                GameObject[] go = GameObject.FindGameObjectsWithTag("Bird");
                for (int i = 0; i < zen_spawn_num && num_of_birds > 0; i++) {
                    if (go[i] != null) EatBird(go[i]);
                    else break;
                }
            }

            // toggle target
            if(Input.GetKeyDown(KeyCode.T)) {
                if (leader.CompareTag("Player") && egg_active) {
                    leader = GameObject.FindGameObjectWithTag("Egg").transform;
                    mouse_leader = false;
                } else {
                    leader = GameObject.FindGameObjectWithTag("Player").transform;
                    mouse_leader = true;
                }
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
    public void ImHere(GameObject bird_go) {
        boids.Add(bird_go.GetComponent<Rigidbody2D>());

        num_of_birds++;
        // Debug.Log(num_of_birds);

        // score increase
        score += egg_hatch_score;
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
    /// preset limits. This method handles having maximum number of birds as well.
    /// </summary>
    public void SpawnEgg() {
        if(num_of_birds < max_bird_num) {
            GameObject go = Instantiate(egg, RandomPointInView(), Quaternion.identity);
            egg_active = true;

            if (zenMode && !mouse_leader)
                leader = go.transform;
        } else {
            egg_active = false;

            if (zenMode) {
                leader = GameObject.FindGameObjectWithTag("Player").transform;
                mouse_leader = true;
            }
        }
    }

    /// <summary>
    /// Called by a monster when it collides with a bird. It eats the bird, which is then destroyed.
    /// This method can be changed to handle events that occur when the number of birds decreases,
    /// such as losing the game.
    /// </summary>
    /// <param name="go">The bird GameObject that is about to be eaten.</param>
    public void EatBird(GameObject go) {
        num_of_birds--;
        Destroy(go);
        // Debug.Log(num_of_birds);

        // score change
        score += bird_eaten_score;

        if (!egg_active) SpawnEgg();
    }

    /// <summary>
    /// Called by a monster when enough birds collide with it. The monster is killed. This method
    /// handles destroying the monster GameObject and score increase.
    /// </summary>
    /// <param name="go"></param>
    public void KillMonster(GameObject go) {
        Destroy(go);

        // score change
        score += defeat_monster_score;
    }


    IEnumerator SpawnMonster() {
        yield return new WaitForSeconds(5);
        while (true) {
            // spawn
            Instantiate(monster, RandomPointInView(), Quaternion.identity);

            yield return new WaitForSeconds(avg_spawn_time + Random.Range(-1f, 1f));
        }
    }

    /// <summary>
    /// Returns a random point in the game plane, visible in the Window.
    /// Use for random spawn locations etc.
    /// </summary>
    /// <returns>Random Vector2 of a point on screen</returns>
    public Vector2 RandomPointInView() {
        float x = Random.value * (rand_max_x - rand_min_x) + rand_min_x;
        float y = Random.value * (rand_max_y - rand_min_y) + rand_min_y;
        return new Vector2(x, y);
    }

    /*
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

    */

    public void PauseGame(bool shouldPause)
    {
        Time.timeScale = shouldPause ? 0f : 1f;
        pauseScreen.SetActive(shouldPause);
        Cursor.lockState = shouldPause ? CursorLockMode.None : CursorLockMode.Confined;
        Cursor.visible = !shouldPause;
        isGamePaused = shouldPause;
    }

    private void GameOver()
    {
        PauseGame(true);
        pauseScreen.SetActive(false);
        gameOverScreen.SetActive(true);
    }

    public void ButtonPressLoadScene(string SceneName)
    {
        // if reload scene
        if (SceneName == "-1")
            SceneName = SceneManager.GetActiveScene().name;

        PauseGame(false);
        SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
    }

}
