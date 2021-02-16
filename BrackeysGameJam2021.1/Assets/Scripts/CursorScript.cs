using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CursorScript : MonoBehaviour
{
    void LateUpdate() {
        // sets Cursor position to the mouse position
        transform.position = Camera.main.ScreenPointToRay(Input.mousePosition).origin;
    }
}
