using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

public class RubyController : MonoBehaviour
{
    // Health Variables
    public int maxHealth = 5;
    public int health { get { return currentHealth; }}
    int currentHealth;

    // Invincibility Timer
    public float timeInvincible = 2.0f;
    bool isInvincible;
    float invincibleTimer;

    // Speed Boost Timer
    public float timeBoosting = 4.0f;
    float speedBoostTimer;
    bool isBoosting;

    // Cog Object and Ammo Variables
    public GameObject projectilePrefab;
    public int ammo { get { return currentAmmo; }}
    public int currentAmmo;

    // Ammo Text UI
    public TextMeshProUGUI ammoText;

    // Ruby Variables
    Rigidbody2D rigidbody2d;
    float horizontal; 
    float vertical;
    public float speed = 5.0f;

    // Animator
    Animator animator;
    Vector2 lookDirection = new Vector2(1,0);

    // Audio Source Variables
    AudioSource audioSource;

    public AudioClip throwSound;
    public AudioClip hitSound;
    public AudioClip tickingSound;
    public AudioSource backgroundManager;


    // Health and Damage Particles
    public ParticleSystem damageEffect;

    // Fixed Robots TMP Integers
    public TextMeshProUGUI fixedText;
    private int scoreFixed = 0;

    // Win text and Lose Text and Restart bool
    public GameObject WinTextObject;
    public GameObject LoseTextObject;
    bool gameOver;
    bool winGame;
    public static int level = 1; // Level manager

    // Timer Variables
    [SerializeField] private TextMeshProUGUI timerUI; // Attach TMP object to this slot
    [SerializeField] private float mainTimer; // Change this value to your liking in Unity

    private float timer;
    private bool canCount = false;
    private bool doOnce = false;
    private bool hasPressedKey = false;
    private bool hasMoved = false;

    public GameObject TimerObject; // This is your Timer text in the Canvas


    // Start is called before the first frame update
    void Start()
    {
        // Framerate and VSync Code
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        // Ruby's Rigidbody Component
        rigidbody2d = GetComponent<Rigidbody2D>();

        // Health
        currentHealth = maxHealth;

        // Animator
        animator = GetComponent<Animator>();

        // Audio Component
        audioSource = GetComponent<AudioSource>();

        // Ammo at start
        rigidbody2d = GetComponent<Rigidbody2D>();
        AmmoText();

        // Fixed Robot Text
        fixedText.text = "Fixed Robots: " + scoreFixed.ToString() + "/4";

        // Win Text and Lose text set to false, as well as restart bool
        WinTextObject.SetActive(false);
        LoseTextObject.SetActive(false);
        gameOver = false;
        winGame = false;

        // Timer
        timer = mainTimer;
        TimerObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        // Movement Variables
        horizontal = Input.GetAxis("Horizontal");
        vertical = Input.GetAxis("Vertical");

        // Animation and Flip
        Vector2 move = new Vector2(horizontal, vertical);
        
        if(!Mathf.Approximately(move.x, 0.0f) || !Mathf.Approximately(move.y, 0.0f))
        {
            lookDirection.Set(move.x, move.y);
            lookDirection.Normalize();

            hasMoved = true; // If player moves, timer will not be able to be activated.
        }
        
        animator.SetFloat("Look X", lookDirection.x);
        animator.SetFloat("Look Y", lookDirection.y);
        animator.SetFloat("Speed", move.magnitude);

        // Invincible Timer
        if (isInvincible)
        {
            invincibleTimer -= Time.deltaTime;
            if (invincibleTimer < 0)
                isInvincible = false;
        }

        // Speed Boost Timer
        if (isBoosting == true)
        {
            speedBoostTimer -= Time.deltaTime; // Once speed boost activates, it counts down
            speed = 8;
        
            if (speedBoostTimer < 0)
            {
                isBoosting = false;
                speed = 5; 
            }
        }

        // Cog Bullet is launched - Ammo in UI is reduced
        if(Input.GetKeyDown(KeyCode.C))
        {
            Launch();
            
            if (currentAmmo > 0)
            {
                ChangeAmmo(-1);
                AmmoText();
            }
        }

        // Talking to NPC
        if (Input.GetKeyDown(KeyCode.X))
        {
            RaycastHit2D hit = Physics2D.Raycast(rigidbody2d.position + Vector2.up * 0.2f, lookDirection, 1.5f, LayerMask.GetMask("NPC"));
            if (hit.collider != null)
            {
                NonPlayerCharacter character = hit.collider.GetComponent<NonPlayerCharacter>();
                if (character != null)
                {
                    if (scoreFixed >= 4)
                    {
                        SceneManager.LoadScene("Level 2");
                        level = 2;
                    }

                    else
                    {
                       character.DisplayDialog(); 
                    }
                }
            }
        }

        // Close Game
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }

        // Restart game
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (gameOver == true)
            {
                // this loads the currently active scene
              SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }

            if (winGame == true)
            {
                SceneManager.LoadScene("Level 1");
                level = 1;
            }
        }

        // Timer Main Code
        // Press this key to activate timer
        if (hasMoved == false)
        {
            if (hasPressedKey == false)
            {
                if (Input.GetKeyDown(KeyCode.T))
                {
                    timer = mainTimer;
                    canCount = true;
                    doOnce = false;
                    TimerObject.SetActive(true);

                    // Ticking Sound Starts
                    PlaySound(tickingSound);
            
                    hasPressedKey = true;
                }
            }
        }

        // Timer functionality
        if (timer >= 0.0f && canCount)
        {
            timer -= Time.deltaTime;
            timerUI.text = "Time: " + timer.ToString("F");
        }

        else if (timer <= 0.0f && !doOnce)
        {
            canCount = false;
            doOnce = true;
            timerUI.text = "Time: " + timer.ToString("0.00");

            // Lose State
            LoseTextObject.SetActive(true);

            transform.position = new Vector3(-5f, 0f, -100f);
            speed = 0;
            Destroy(gameObject.GetComponent<SpriteRenderer>());

            gameOver = true;

            // BackgroundMusicManager is turned off
            backgroundManager.Stop();

            // Calls sound script and plays lose sound
            SoundManagerScript.PlaySound("GameOver");

            // Ticking Sound Stops
            StopSound(tickingSound);
        }
    }

    void FixedUpdate()
    {
        // Movement Code (Speed Value can be changed in Unity)
        Vector2 position = rigidbody2d.position;
        position.x = position.x + speed * horizontal * Time.deltaTime;
        position.y = position.y + speed * vertical * Time.deltaTime;

        rigidbody2d.MovePosition(position);
    }

    public void ChangeHealth(int amount)
    {
        // Invincible
        if (amount < 0)
        {
            if (isInvincible)
                return;
            
            isInvincible = true;
            invincibleTimer = timeInvincible;
            

            PlaySound(hitSound);

            animator.SetTrigger("Hit");

            // Damage Particle effect 
            damageEffect = Instantiate(damageEffect, rigidbody2d.position + Vector2.up * 0.5f, Quaternion.identity);
        }

        // Ruby loses all health, lose text appears and restart becomes true
        if (currentHealth == 1)
        {
            LoseTextObject.SetActive(true);

            transform.position = new Vector3(-5f, 0f, -100f);
            speed = 0;
            Destroy(gameObject.GetComponent<SpriteRenderer>());

            gameOver = true;

            // BackgroundMusicManager is turned off
            backgroundManager.Stop();

            // Calls sound script and plays lose sound
            SoundManagerScript.PlaySound("GameOver");
        }

        // Health math code
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        Debug.Log(currentHealth + "/" + maxHealth);

        UIHealthBar.instance.SetValue(currentHealth / (float)maxHealth);
    }

    // Ammo Function
    public void ChangeAmmo(int amount)
    {
        // Ammo math code
        currentAmmo = Mathf.Abs(currentAmmo + amount);
        Debug.Log("Ammo: " + currentAmmo);
    }

    public void AmmoText()
    {
        ammoText.text = "Ammo: " + currentAmmo.ToString();
    }

    // Projectile Code
    void Launch()
    {
        if (currentAmmo > 0) // If player has ammo, they can launch cogs
        {
            GameObject projectileObject = Instantiate(projectilePrefab, rigidbody2d.position + Vector2.up * 0.5f, Quaternion.identity);

            Projectile projectile = projectileObject.GetComponent<Projectile>();
            projectile.Launch(lookDirection, 300);

            animator.SetTrigger("Launch");

            PlaySound(throwSound);
        }
    }

    // Plays sounds from this script and others
    public void PlaySound(AudioClip clip)
    {
        audioSource.PlayOneShot(clip);
    }

    public void StopSound(AudioClip clip)
    {
        audioSource.Stop();
    }

    public void FixedRobots(int amount)
    {
        scoreFixed += amount;
        fixedText.text = "Fixed Robots: " + scoreFixed.ToString() + "/4";

        Debug.Log("Fixed Robots: " + scoreFixed);

        // Talk to Jambi to visit stage 2
        if (scoreFixed == 4 && level == 1)
        {
            WinTextObject.SetActive(true);

            // Disables time attack on level
            canCount = false;
            doOnce = true;
            TimerObject.SetActive(false);

            // Ticking Sound Stops
            StopSound(tickingSound);
        }

        // Win Text Appears ONLY if on Level 2
        if (scoreFixed == 4 && level == 2)
        {
            WinTextObject.SetActive(true);

            winGame = true;

            transform.position = new Vector3(-5f, 0f, -100f);
            speed = 0;

            Destroy(gameObject.GetComponent<SpriteRenderer>());

            // BackgroundMusicManager is turned off
            backgroundManager.Stop();

            // Calls sound script and plays win sound
            SoundManagerScript.PlaySound("FFWin");

            // Disables time attack on level
            canCount = false;
            doOnce = true;
            TimerObject.SetActive(false);

            // Ticking Sound Stops
            StopSound(tickingSound);
        }
        
    }

    public void SpeedBoost(int amount)
    {
        if (amount > 0)
        {
            speedBoostTimer = timeBoosting;
            isBoosting = true;
        }
    }
}
