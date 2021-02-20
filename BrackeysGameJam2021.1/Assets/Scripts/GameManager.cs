using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameManager : MonoBehaviour
{
    // [UnityEngine.HideInInspector]
    // [UnityEngine.InspectorName("test")]
    // [UnityEngine.Min]
    // [UnityEngine.Range]
    // [UnityEngine.RuntimeInitializeOnLoadMethod]
    // [UnityEngine.SerializeField]
    // [Space]
    // [UnityEngine.AddComponentMenu("A/B")]

    // game settings
    [Header("Game Settings")]
    public bool dev_mode_active = false;
    public int debug_spawn_num = 1;
    public bool zenMode = false;

    // game statistics
    [Header("Game Statistics")]
    public int max_bird_num = 50;
    private int num_of_birds = 0;
    public int numberOfBirds = 0;

    [Header("Main Variables")]
    public Transform leader = null;     // target transform
    public GameObject bird = null;      // bird object instance (for spawning)
    public GameObject egg = null;       // egg object instance (for spawning)
    public GameObject monster = null;   // monster objsect instance (for spawning)
    private bool egg_active = false;    // true if is there at least one egg on the screen

    // window edges
    private float rand_min_x = -8.5f;
    private float rand_max_x = 8.5f;
    private float rand_min_y = -3.5f;
    private float rand_max_y = 4.5f;

    // zen mode
    [Header("Zen mode")]
    [SerializeField]
    private int zen_spawn_num = 10;
    [SerializeField]
    private int zen_max_bird_num = 100;
    private bool mouse_leader = true;
    
    // monster details
    [Header("Monster details")]
    [SerializeField]
    private float avg_spawn_time = 10;

    // bird details
    [Header("Bird details")]
    public float max_follow_speed = 1;
    public float max_avoid_speed = 1;

    // boids algorithm attributes
    [Header("Boids Algorithm Attributes")]
    public int max_neighbors_num = 20;          // maximal number of neighbors taken into account when computing velocity
    public float max_distance = 1;              // maximal distance from neighbor
    public float follow_threshold = 0.5f;       // threshold for distance from leader, at which the status shifts to avoid
    public float avoid_threshold = 1;           // threshold for distance from leader, at which the status shifts to follow
    public float follow_weight = 0;             // importance of following the leader
    public float steering_weight = 0;           // importance of steering to right direction
    public float cohesion_normal_weight = 0;    // importance of cohesion durning non-attack modes
    public float cohesion_attack_weight = 0;    // importance of cohesion durning attack mode 
    public float steps = 100;                   // used with cohesion, 100 means 1% towards the center of the group
    public float separation_weight = 0;         // importance of the separation of flock
    public float alignment_weight = 0;          // importance of birds flight direction

    // game score
    [Header("Game score")]
    public int score = 0;
    [SerializeField]
    private int egg_hatch_score = 1;
    [SerializeField]
    private int bird_eaten_score = 0;
    [SerializeField]
    private int defeat_monster_score = 1;

    // UI references
    [Header("UI")]
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
    private GameObject gameOverScreen;
    [SerializeField]
    private GameObject pauseScreen;
    private bool isGamePaused = false;

    void Start() {
        // game settings
        // Screen.SetResolution(1920, 1080, false);
        // QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;

        // Set mouse cursor to not be visible and lock it the screen
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;

        // game starting conditions
        // making sure that a leader exists
        if (leader == null) leader = GameObject.FindGameObjectWithTag("Player").transform;
        // spawn the first egg
        SpawnEgg();

        // check game mode
        if (zenMode) max_bird_num = zen_max_bird_num;
        else StartCoroutine("SpawnMonster");            // start spawning monsters

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
        if (Input.GetKeyDown(KeyCode.Escape)) {
            isGamePaused = !isGamePaused;
            PauseGame(isGamePaused);
        }
        
        // check for game over
        if (num_of_birds <= 0f && !zenMode)
            GameOver();

        // check for development mode commands
        if(dev_mode_active && !zenMode) {
            if (Input.GetKeyDown(KeyCode.S) && bird != null) {
                // spawn a bird at (0,i)
                Debug.Log("SPAWN BIRD");
                for (int i = 0; i < debug_spawn_num; i++) {
                    Instantiate(bird, Vector2.up * i, Quaternion.identity);
                }
            } else if (dev_mode_active && Input.GetKeyDown(KeyCode.E) && egg != null) {
                // spawn an egg at (0,0)
                Debug.Log("SPAWN EGG");
                Instantiate(egg, Vector2.zero, Quaternion.identity);
            } else if (dev_mode_active && Input.GetKeyDown(KeyCode.D) && egg != null) {
                Debug.Log("SPAWN EGGS");
                for (int i = 0; i < debug_spawn_num; i++) {
                    SpawnEgg();
                }
            }
        }

        // check for zen mode commands
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
    }


    /***************************************************************************************
     *                          METHODS
     ***************************************************************************************/

    /// <summary>
    /// Called by birds, it notifies the GameManager that a new bird has been spawned
    /// and should be accounted for
    /// </summary>
    /// <param name="bird">The newly created bird</param>
    public void ImHere(GameObject bird_go) {
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
    /// preset limits. This method handles having maximum number of birds as well
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
    /// such as losing the game
    /// </summary>
    /// <param name="go">The bird GameObject that is about to be eaten</param>
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
    /// handles destroying the monster GameObject and score increase
    /// </summary>
    /// <param name="go">GameObject of the killed moster</param>
    public void KillMonster(GameObject go) {
        Destroy(go);

        // score change
        score += defeat_monster_score;
    }

    /// <summary>
    /// Coroutine responsible for monster spawning.
    /// </summary>
    /// <returns>Time until the next monster spawns</returns>
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


    /***************************************************************************************
     *                          UI METHODS
     ***************************************************************************************/

    /// <summary>
    /// Pauses or unpauses the game, based on the <c>shouldPause</c> parameter. The method
    /// handles setting the time scale, cursor and UI
    /// </summary>
    /// <param name="shouldPause">True to pause the game, false to unpause</param>
    public void PauseGame(bool shouldPause)
    {
        Time.timeScale = shouldPause ? 0f : 1f;
        pauseScreen.SetActive(shouldPause);
        Cursor.lockState = shouldPause ? CursorLockMode.None : CursorLockMode.Confined;
        Cursor.visible = !shouldPause;
        isGamePaused = shouldPause;
    }

    /// <summary>
    /// Notifies the user that the game is over. The game is paused and the gameOverScreen is
    /// shown
    /// </summary>
    private void GameOver()
    {
        PauseGame(true);
        pauseScreen.SetActive(false);
        gameOverScreen.SetActive(true);
    }

    /// <summary>
    /// Loads a new scene or reloads the same scene
    /// </summary>
    /// <param name="SceneName">Name of the scene to load. Set to <c>"-1"</c> to reload</param>
    public void ButtonPressLoadScene(string SceneName)
    {
        // if reload scene
        if (SceneName == "-1")
            SceneName = SceneManager.GetActiveScene().name;

        PauseGame(false);
        SceneManager.LoadScene(SceneName, LoadSceneMode.Single);
    }

}
