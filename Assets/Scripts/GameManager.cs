using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager singleton = null;

    public static bool blockFalling;

    public Camera mainCam;
    public GameObject ghostBlock;
    public GameObject currentHighestLine;
    public GameObject recordhighestLine;
    public GameObject tower; // Empty gameobject used to child blocks to
    public List<GameObject> blocks; // List of blocks that can be spawned/controlled
    public List<GameObject> ghostBlocks; // List of ghost blocks to be references by the above
    public List<GameObject> towerBlocks; // A list of blocks that have landed and are currently part of the tower
    public List<AudioSource> soundFX;

    public Image[] lifeSprites;
    public GameObject endGamePanel;
    public GameObject pausePanel;
    public Text highScoreText;
    public Text scoretext;

    public int lives;
    public float lifeLostDelayTime; // Short delay to prevent a batch of blocks falling at the same time from losing multiple lives (Set to [5] in Editor)
    private bool lifeLostDelay;
    public float blockSpawnOffset; // Offset to spawn the blocks offscreen (Set to [25] in Editor)
    public float currentHighestPoint;
    public float fallSpeed; // Set to [5] in the Editor
    public float rotateSpeed; // Set to [3] in the Editor
    public float moveSpeed; // Set to [5] in the Editor
    public float fallSpeedIncrease; // How often the fallspeed increases (Set to [60] in Editor)
    public float cameraSmoothing; // How quick, in seconds, the camera moves (Set to [0.25] in Editor)

    private Vector3 spawnPos; // Block spawn point
    private Vector3 camOffset;
    public float renderOffset; // Offset to decide how far off screen a block needs to be before we can start ignoring/deleting it (set to [5] in Editor)

    public bool gameActive;

    static public int highscore;
    private int score;

    void Awake()
    {
        if (singleton == null)
        {
            singleton = this;
        }
        else if (singleton != this)
        {
            Destroy(gameObject);
        }

        highscore = PlayerPrefs.GetInt("Highscore", highscore);
    }

    /// <summary>
    /// Initialise the block spawn point and 'record height' start point when the scene loads
    /// </summary>
	void Start()
    {
        camOffset = mainCam.transform.position;
        spawnPos = new Vector3(0, mainCam.transform.position.y + blockSpawnOffset, 0);

        currentHighestLine.transform.position = new Vector3(0, 0, 0);
        recordhighestLine.transform.position = new Vector3(0, highscore, 0);

        score = 0;
        scoretext.text = "Score: " + score;
        highScoreText.text = "Highest: " + highscore;

        gameActive = true;
    }

    void FixedUpdate()
    {
        if (gameActive)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Pause(true);
            }

            if (fallSpeed < 25)
            {
                Invoke("IncreaseFallSpeed", fallSpeedIncrease);
            }

            if (!blockFalling) // If a block has just landed
            {
                spawnPos = new Vector3(0, mainCam.transform.position.y + blockSpawnOffset, 0); // Update the block spawn point based on where the camera currently is

                int rand = Random.Range(0, blocks.Count); // Spawn another random block
                GameObject newBlock = Instantiate(blocks[rand], spawnPos, transform.rotation);
                newBlock.GetComponent<Block>().meshID = rand;

                blockFalling = true;
            }
        }
    }

    /// <summary>
    /// Move the Camera based on how the current tower stands
    /// </summary>
    /// <param name="block"></param> Wether the block is falling lower than expected (the tower is toppling)
    /// <param name="block"></param> The block placed/falling
	public void UpdateCamera(bool blockDropped, GameObject block)
    {
        if(!blockDropped)
        {
            if(block.transform.position.y > mainCam.transform.position.y - camOffset.y)
            {
                if (block.transform.position.y > currentHighestPoint) // If the block landed higher than our current record then update the record
                {
                    currentHighestLine.transform.position = new Vector3(0, block.transform.position.y, 0);
                    currentHighestPoint = currentHighestLine.transform.position.y;
                    score = Mathf.RoundToInt(currentHighestPoint);
                    scoretext.text = "Score: " + score;                    
                }
                StartCoroutine(CameraLerp(mainCam.transform.position, new Vector3(0, block.transform.position.y, 0) + camOffset));
            }
        }
        else
        {
            GameObject highestBlockObj = null;
            float highestBlock = 0;

            foreach (GameObject blockCheck in towerBlocks) // Check each block in the tower to see which one is currently the highest
            {
                if (block != blockCheck && blockCheck.GetComponent<Block>().height > highestBlock)
                {
                    highestBlock = blockCheck.GetComponent<Block>().height;
                    highestBlockObj = blockCheck;
                }
            }

            if (highestBlockObj != null) // If we have a block higher than the one that just fell, update the camera to give the player more space
            {
                StopCoroutine("CameraLerp");
                StartCoroutine(CameraLerp(mainCam.transform.position, new Vector3(0, highestBlockObj.transform.position.y, 0) + camOffset));
            }
        }
    }

    /// <summary>
    /// Remove a life any time a block falls off screen and handle the game over screen
    /// </summary>
	public void UpdateLives()
    {
        if (!lifeLostDelay)
        {
            soundFX[1].Play();
            lives--;
            lifeSprites[lives].fillAmount = 0;

            if (lives == 0)
            {
                endGamePanel.SetActive(true);
                gameActive = false;
                blockFalling = false;
                Time.timeScale = 0;

                if (score > highscore)
                {
                    highscore = score;
                    PlayerPrefs.SetInt("Highscore", highscore);
                    highScoreText.text = "Highest: " + highscore;
                }
            }

            lifeLostDelay = true;
            StartCoroutine(LiveDelaytimer(lifeLostDelayTime));
        }
    }

    void IncreaseFallSpeed()
    {
        fallSpeed += 1;
        CancelInvoke();
    }

    public void NewGame()
    {
        soundFX[1].Play();
        Time.timeScale = 1;
        SceneManager.LoadScene(1);
    }

    public void Pause(bool paused)
    {
        if (paused)
        {
            pausePanel.SetActive(true);
            Time.timeScale = 0;
        }
        else
        {
            pausePanel.SetActive(false);
            Time.timeScale = 1;
        }
    }

    public void Quit()
    {
        soundFX[1].Play();
        Application.Quit();
    }

    IEnumerator LiveDelaytimer(float timer)
    {
        yield return new WaitForSeconds(timer);
        lifeLostDelay = false;
    }

    IEnumerator CameraLerp(Vector3 start, Vector3 target)
    {
        float t = 0;

        while (t < cameraSmoothing)
        {
            t += Time.deltaTime;

            mainCam.transform.position = Vector3.Lerp(start, target, t);
            yield return 0;
        }
    }
}
