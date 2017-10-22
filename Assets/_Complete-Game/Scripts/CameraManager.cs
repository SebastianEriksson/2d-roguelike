using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Completed;

public class CameraManager : MonoBehaviour {

    // Public variable to store the player game object
	public Player player;

    // Private variable to store the offset between the camera and player
    private Vector3 offset;

    // Use this for initialization
    void Start ()
    {
        // Get the distance between the player and camera
        offset = transform.position - player.transform.position;
        offset.x = 0;
        offset.y = 0;
    }
    
    // Update is called once per frame
    void Update ()
    {
		if (player.ExitReached) { return; }
        // Set the position of the cameras transform to be the same as the player, but offset by the calculated offset distance
        transform.position = player.transform.position + offset;
    }
}
