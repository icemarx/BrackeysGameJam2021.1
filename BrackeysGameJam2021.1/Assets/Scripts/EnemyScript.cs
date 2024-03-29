using System.Collections;
using UnityEngine;

public class EnemyScript : MonoBehaviour
{
    GameManager gm;

    // movement
    [SerializeField]
    private float avg_wait_time = 5;
    [SerializeField]
    private float jump_force_mod = 1;
    [SerializeField]
    private Transform target;
    private Rigidbody2D rb;

    // status
    private const int MOVING = 0;
    private const int JUMPING = 1;
    private const int FALLING = 2;
    private const int WAITING = 3;
    int status = FALLING;

    // eating
    [SerializeField]
    private float reach = 1;        // how far does the monster reach to eat birds
    [SerializeField]
    private int num_birds_to_die = 20;   // number of birds needed in proximity for the monster to die
    private int eaten = 0;          // number of birds eaten by the monster in this jump
    private int all_eaten = 0;      // number of birds eaten by the monster


    void Start() {
        rb = GetComponent<Rigidbody2D>();
        gm = FindObjectOfType<GameManager>();
        target = gm.leader;

        num_birds_to_die = Mathf.FloorToInt(gm.MonsterToKillNumber);  // TODO: improve this
    }
    
    private void Update() {
        Collider2D[] col = new Collider2D[num_birds_to_die];
        int num_neighbors = Physics2D.OverlapCircleNonAlloc(transform.position, reach, col, 1 << LayerMask.NameToLayer("BirdLayer"));

        if (num_neighbors >= num_birds_to_die) {
            // kill monster
            // Debug.Log("Kill");
            gm.KillMonster(gameObject);
            // } else if (status == JUMPING) {
        } else {
            // eating mechanic
            if(num_neighbors > 0) {
                for(int i = 0; i < num_neighbors && eaten < gm.max_to_eat; i++) {
                    if (col[i].CompareTag("Bird")) {
                        // eat that bird
                        gm.EatBird(col[i].gameObject);
                        eaten++;
                        all_eaten++;
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Checks if the collided objects is the ground, in which case, the monster prepares
    /// to jump.
    /// <see cref="PrepareJump"/>
    /// </summary>
    /// <param name="collision">Collider the monster collided with</param>
    private void OnCollisionEnter2D(Collision2D collision) {
        if (collision.gameObject.CompareTag("Ground")) {
            status = WAITING;
            StartCoroutine("PrepareJump");
        }
    }

    private void OnCollisionExit2D(Collision2D collision) {
        if (collision.gameObject.CompareTag("Ground")) {
            status = JUMPING;
            StopCoroutine("PrepareJump");
        }
    }

    /// <summary>
    /// Prepares the Monster to jump after some random (averaged) time
    /// </summary>
    /// <returns>Time until the jump</returns>
    private IEnumerator PrepareJump() {
        while(status == WAITING) {
            float my_waiting_time = avg_wait_time + Random.Range(-1f, 1f);
            yield return new WaitForSeconds(my_waiting_time);

            // Debug.Log("JUMP!!!");
            eaten = 0;
            rb.velocity = (target.position - transform.position) * jump_force_mod;
        }
    }
}
