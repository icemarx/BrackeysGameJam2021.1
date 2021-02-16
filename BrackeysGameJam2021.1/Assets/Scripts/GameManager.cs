using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    void Start() {
        // Set mouse cursor to not be visible
        Cursor.visible = false;

        // lock cursor to screen
        Cursor.lockState = CursorLockMode.Confined;
    }
}
