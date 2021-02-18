using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EggScript : MonoBehaviour
{
    public GameObject bird = null;
    private Collider2D[] col = new Collider2D[1];

    private void Update() {
        int num = Physics2D.OverlapCircleNonAlloc(transform.position, 0.1f, col, 1 << LayerMask.NameToLayer("BirdLayer"));

        if(num > 0 && col[0].gameObject.CompareTag("Bird")) {
            // Hatch
            Instantiate(bird, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}
