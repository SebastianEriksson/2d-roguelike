using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

namespace Completed
{
    using System.Collections.Generic;		//Allows us to use Lists. 
    using UnityEngine.UI;					//Allows us to use UI.
    
    public class GameManager : MonoBehaviour
    {
        public float levelStartDelay = 2f;						//Time to wait before starting level, in seconds.
        public float turnDelay = 0.1f;							//Delay between each Player turn.
        public int playerFoodPoints = 100;						//Starting value for Player food points.
        public static GameManager instance = null;				//Static instance of GameManager which allows it to be accessed by any other script.
        [HideInInspector] public bool playersTurn = true;		//Boolean to check if it's players turn, hidden in inspector but public.
        public bool gameOver = false;                           // Initialize game over, set to false at beginning
        

        private Text levelText;									//Text to display current level number.
        private Text scoreText;									//Text to display current level and the high score during the game.
        private GameObject levelImage;							//Image to block out level as levels are being set up, background for levelText.
        private MapGenerator map;
        private int level = 1;									//Current level number, expressed in game as "Day 1".
        private List<Enemy> enemies;							//List of all Enemy units, used to issue them move commands.
        private bool enemiesMoving;								//Boolean to check if enemies are moving.
        private bool doingSetup = true;							//Boolean to check if we're setting up board, prevent Player from moving during setup.
        
        public static MapGenerator Map {
            get { return instance.map; }
        }
        
        //Awake is always called before any Start functions
        void Awake()
        {
            //Check if instance already exists
            if (instance == null)

                //if not, set instance to this
                instance = this;

            //If instance already exists and it's not this:
            else if (instance != this)

                //Then destroy this. This enforces our singleton pattern, meaning there can only ever be one instance of a GameManager.
                Destroy(gameObject);	
            
            //Sets this to not be destroyed when reloading scene
            DontDestroyOnLoad(gameObject);
            
            //Assign enemies to a new List of Enemy objects.
            enemies = new List<Enemy>();
            
            map = GetComponent<MapGenerator>();
            
            //Call the InitGame function to initialize the first level 
            InitGame();
        }

        //this is called only once, and the paramter tell it to be called only after the scene was loaded
        //(otherwise, our Scene Load callback would be called the very first load, and we don't want that)
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static public void CallbackInitialization()
        {
            //register the callback to be called everytime the scene is loaded
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        //This is called each time a scene is loaded.
        static private void OnSceneLoaded(Scene arg0, LoadSceneMode arg1)
        {
            instance.level++;
            instance.InitGame();
        }

        
        //Initializes the game for each level.
        void InitGame()
        {
            //While doingSetup is true the player can't move, prevent player from moving while title card is up.
            doingSetup = true;
            
            //Get a reference to our image LevelImage by finding it by name.
            levelImage = GameObject.Find("LevelImage");
            
            //Get a reference to our text LevelText's text component by finding it by name and calling GetComponent.
            levelText = GameObject.Find("LevelText").GetComponent<Text>();
            
            //Set the text of levelText to the string "Day" and append the current level number.
            levelText.text = "Day " + level;

            //Get the high score.
            int highscore = PlayerPrefs.GetInt("highscore", 1);
            if (level > highscore) { highscore = level; }

            //Level status text (current and highscore display).
            scoreText = GameObject.Find("ScoreText").GetComponent<Text>();
            scoreText.text = "Day: " + level + "  Best: " + highscore;
            
            //Set levelImage to active blocking player's view of the game board during setup.
            levelImage.SetActive(true);
            
            //Call the HideLevelImage function with a delay in seconds of levelStartDelay.
            Invoke("HideLevelImage", levelStartDelay);
            
            //Clear any Enemy objects in our List to prepare for next level.
            enemies.Clear();
            
            map.MakeNewMap(level);
            
        }
        
        
        //Hides black image used between levels
        void HideLevelImage()
        {
            //Disable the levelImage gameObject.
            levelImage.SetActive(false);
            
            //Set doingSetup to false allowing player to move again.
            doingSetup = false;
        }

        //Update is called every frame.
        void Update()
        {

            // if game over state is set to true, allow player to restart the game
            if (gameOver == true && Input.GetMouseButtonDown(0))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                
                // Reset day count to day 1
                instance.level = 0;

                // Start the music again
                SoundManager.instance.musicSource.Play();

                // Make sure the game over state is set back to false when restarting the game
                gameOver = false;

            }

            //Check that playersTurn or enemiesMoving or doingSetup are not currently true.
            if (playersTurn || enemiesMoving || doingSetup)

                //If any of these are true, return and do not start MoveEnemies.
                return;

            //Start moving enemies.
            StartCoroutine (MoveEnemies ());

        }
        
        //Call this to add the passed in Enemy to the List of Enemy objects.
        public void AddEnemyToList(Enemy script)
        {
            //Add Enemy to List enemies.
            enemies.Add(script);
        }
        
        
        //GameOver is called when the player reaches 0 food points
        public void GameOver()
        {
            // Set game over state to true
            gameOver = true;

            //Set levelText to display number of levels passed and game over message
            levelText.text = "After " + level + " days, you starved.\n\nPress mouse1 to restart";

            //Save if new high score
            if (level > PlayerPrefs.GetInt("highscore", 1))
            {
                PlayerPrefs.SetInt("highscore", level);
                PlayerPrefs.Save();
            }
            
            //Enable black background image gameObject.
            levelImage.SetActive(true);
            
            //Disable this GameManager.
            enabled = true;

            // Enter setup for the duration of the game over scene. This allows us to block the player from moving while being dead
            doingSetup = true;
        }
        
        //Coroutine to move enemies in sequence.
        IEnumerator MoveEnemies()
        {
            //While enemiesMoving is true player is unable to move.
            enemiesMoving = true;
            
            //Wait for turnDelay seconds, defaults to .1 (100 ms).
            yield return new WaitForSeconds(turnDelay);
            
            //Loop through List of Enemy objects.
            for (int i = 0; i < enemies.Count; i++)
            {
                enemies[i].MoveEnemy();
            }

            yield return new WaitForSeconds(turnDelay);

            //Once Enemies are done moving, set playersTurn to true so player can move.
            playersTurn = true;
            
            //Enemies are done moving, set enemiesMoving to false.
            enemiesMoving = false;
        }
    }
}

