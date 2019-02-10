using UnityEngine;

public class Block : MonoBehaviour 
{
    public int meshID; // This is saved as the random number used to spawn a block 
    public GameObject ghost = null;
    public bool landed = false;

    private Rigidbody rb;
	public Transform[] singleBlocks; // The 4 blocks, used for raycast checks to position the ghost block

    public float landedHeight;
    public float height;
    public bool setHeight = false;
	
	void Start()
	{
		rb = GetComponent<Rigidbody>();
        singleBlocks = GetComponentsInChildren<Transform>();

		ghost = Instantiate(GameManager.singleton.ghostBlocks[meshID]);
        GameManager.singleton.ghostBlock = ghost;
	}

	void Update () 
	{
        if(GameManager.singleton.gameActive)
        {
            if (!landed)
            {
                MoveBlock();
                UpdateGhostBlock();
            }
            else
            {
                Vector3 distance = transform.position - Vector3.zero;
                height = Mathf.Abs(distance.y);

                if (!setHeight)
                {
                    landedHeight = Mathf.Abs(distance.y);
                    setHeight = true;
                }

                if (height < landedHeight - GameManager.singleton.renderOffset) // If the block ever falls from where it landed, update the camera to find the highest block
                {
                    landedHeight = height;
                    GameManager.singleton.UpdateCamera(true, gameObject);
                }
                BelowFloorCheck(true);
            }
        }
		
    }
		
	void OnCollisionEnter(Collision col)
	{
		if (!landed) // If the this is the block we're controlling
		{
			DisableControl();
            GameManager.singleton.soundFX[2].Play();
            GameManager.singleton.UpdateCamera (false, gameObject);
        }
    }

    /// <summary>
    /// Handle keypresses for movement and rotation of the acive block
    /// </summary>
	void MoveBlock()
	{
        BelowFloorCheck(false);

        Vector3 movement = new Vector3 (Input.GetAxis ("Horizontal") * GameManager.singleton.moveSpeed, -GameManager.singleton.fallSpeed, 0);

		if (Input.GetKey (KeyCode.Q)) 
		{
			transform.Rotate(new Vector3(0,0,GameManager.singleton.rotateSpeed));
		}
		if (Input.GetKey (KeyCode.E)) 
		{
			transform.Rotate(new Vector3(0,0,-GameManager.singleton.rotateSpeed));
		}

		transform.Translate (movement * Time.deltaTime, Space.World);
	}

    /// <summary>
    /// Disable any movement based on keypresses for this block, add it to the tower and cleanup the ghost cube
    /// </summary>
	void DisableControl()
    {
        rb.velocity = Vector3.zero;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.None;

        gameObject.layer = 0;

        transform.parent = GameManager.singleton.tower.transform;
        landed = true;

        GameManager.singleton.towerBlocks.Add(gameObject);
        GameManager.blockFalling = false;

        Destroy(ghost);
        ghost = null;
    }

    /// <summary>
    /// Position the Ghost Block to reflect where the active block will land
    /// </summary>
    void UpdateGhostBlock()
	{
        RaycastHit rayHit;

		float distToFloor = 100;
		float shortestDistToFloor = 100;

		foreach(Transform block in singleBlocks)
		{
			if (Physics.BoxCast(block.position, new Vector3(block.localScale.x / 2, block.localScale.y / 2, block.localScale.z / 2), -Vector3.up, out rayHit, Quaternion.identity, 50f))
            {
                if (rayHit.transform.gameObject.layer == 0)
                {
                    distToFloor = rayHit.distance;
                    
                    if (distToFloor < shortestDistToFloor) // Check which block would collide first
                    {
                        shortestDistToFloor = distToFloor;
                        GameManager.singleton.ghostBlock.transform.position = new Vector3 (transform.position.x, transform.position.y - rayHit.distance, transform.position.z);
                        GameManager.singleton.ghostBlock.transform.eulerAngles = transform.rotation.eulerAngles;
                    }
				}
			}
		}
    }

    /// <summary>
    /// Check if the block has fallen offscreen
    /// </summary>
    /// <param name="expectedFall"> - Wether or not the block actually landed first, or is the player just let it drop</param>
    void BelowFloorCheck(bool expectedFall)
    {
        if (transform.position.y <= -GameManager.singleton.renderOffset)
        {
            if(!expectedFall)
            {
                DisableControl();
            }

            GameManager.singleton.towerBlocks.Remove(gameObject);
            Destroy(gameObject);

            GameManager.singleton.UpdateLives();
        }
    }
}
