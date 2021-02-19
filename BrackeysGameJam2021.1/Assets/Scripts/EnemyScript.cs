using System.Collections;
using UnityEngine;

public class EnemyScript : MonoBehaviour
{
    GameManager gm;

    // movement
    public float speed = 1;
    public float speed_mod = 0.001f;
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
    [SerializeField]
    private int max_to_eat = 5;     // maximal number that the monster can eat in one jump
    private int eaten = 0;          // number of birds eaten by the monster in this jump
    private int all_eaten = 0;      // number of birds eaten by the monster


    void Start() {
        rb = GetComponent<Rigidbody2D>();
        gm = FindObjectOfType<GameManager>();
        target = gm.leader;
    }
    
    private void Update() {
        Collider2D[] col = new Collider2D[num_birds_to_die];
        int num_neighbors = Physics2D.OverlapCircleNonAlloc(transform.position, reach, col, 1 << LayerMask.NameToLayer("BirdLayer"));

        if (num_neighbors >= num_birds_to_die) {
            Debug.Log("Kill");
            // kill monster
            gm.KillMonster(gameObject);
        } else if (status == JUMPING) {
            // eating mechanic
            if(num_neighbors > 0) {
                for(int i = 0; i < num_neighbors && eaten < max_to_eat; i++) {
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

    private void OnCollisionEnter2D(Collision2D collision) {
        if (collision.gameObject.CompareTag("Ground")) {
            status = WAITING;
            StartCoroutine("PrepareJump");
        }
    }

    private IEnumerator PrepareJump() {
        float my_waiting_time = avg_wait_time + Random.Range(-1f, 1f);
        yield return new WaitForSeconds(my_waiting_time);

        // Debug.Log("JUMP!!!");
        status = JUMPING;
        eaten = 0;
        rb.velocity = (target.position - transform.position) * jump_force_mod;
    }

    /*
private void LateUpdate() {
    Vector3 movement_direction = Vector3.zero;
    switch(status) {
        case MOVING:
            if (go_left) {
                movement_direction = Vector3.left * speed * speed_mod;

                // change status
                if (target.x >= transform.position.x) {
                    status = JUMPING;
                    eaten = 0;
                }
            } else {
                movement_direction = Vector3.right * speed * speed_mod;

                // change status
                if (target.x < transform.position.x) {
                    status = JUMPING;
                    eaten = 0;
                }
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
*/
}
