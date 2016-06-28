using UnityEngine;

public class Controller : MonoBehaviour {
	
	// public variables
	public float jumpForce = 30.0f;
    public float forwardForce = 3.0f;
	public float gravity = 9.81f;
    
    private bool grounded;
    private Rigidbody myBody;
    private Vector3 originalPosition;

	// Use this for initialization
	void Start () {
		// store a reference to the CharacterController component on this gameObject
		// it is much more efficient to use GetComponent() once in Start and store
		// the result rather than continually use etComponent() in the Update function
		myBody = gameObject.GetComponent<Rigidbody>();
        originalPosition = new Vector3(5.0f, 2.0f, 3.0f);
        grounded = true;
    }
	
	// Update is called once per frame
    void FixedUpdate() {
        // Determine how much should move in the z-direction
        Vector3 movementZ = GvrViewer.Instance.HeadPose.Orientation * Vector3.forward * forwardForce;
        Vector3 movementY = Vector3.up * jumpForce;
        Vector3 forceVector = transform.TransformDirection(movementY + movementZ);

        Debug.DrawLine(transform.localPosition, forceVector, Color.red);
        Debug.DrawLine(transform.localPosition, movementZ, Color.blue);
        
        if (Input.GetButtonDown("Jump") || GvrViewer.Instance.Triggered) {
            if (grounded) {
                myBody.AddForce(forceVector, ForceMode.Impulse);
            }
        }

        //if vertical velocity is 0 we're grounded and we can jump again
        if (myBody.IsSleeping()) {
            grounded = true;
        } else {
            grounded = false;
        }

        //check if below the map, reset position
        if (transform.position.y < -5.0f) {
            transform.position = originalPosition;
            grounded = true;
        }
    }

    
}
