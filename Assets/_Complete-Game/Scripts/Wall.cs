using UnityEngine;
using System.Collections;

namespace Completed
{
	public class Wall : BlockingObject
	{
		public AudioClip chopSound1;				//1 of 2 audio clips that play when the wall is attacked by the player.
		public AudioClip chopSound2;				//2 of 2 audio clips that play when the wall is attacked by the player.
		public Sprite dmgSprite;					//Alternate sprite to display after Wall has been attacked by player.
		public int hp = 3;							//hit points for the wall.
		public float chanceOfFood = 0.4f;			//Chance of dropping food after destroying the wall.
		public GameObject foodPrefab;				//Food tile.

		private SpriteRenderer spriteRenderer;		//Store a component reference to the attached SpriteRenderer.
		
		
		void Awake ()
		{
			//Get a component reference to the SpriteRenderer.
			spriteRenderer = GetComponent<SpriteRenderer> ();
		}
		
		
		//DamageWall is called when the player attacks a wall.
		public void DamageWall (int loss)
		{
			//Call the RandomizeSfx function of SoundManager to play one of two chop sounds.
			SoundManager.instance.RandomizeSfx (chopSound1, chopSound2);
			
			//Set spriteRenderer to the damaged wall sprite.
			spriteRenderer.sprite = dmgSprite;
			
			//Subtract loss from hit point total.
			hp -= loss;
			
			//If hit points are less than or equal to zero, destroy the wall
			if(hp <= 0)
				GetDestroyed ();
		}

		// Called when all health points of the wall are lost.
		private void GetDestroyed ()
		{
			//Disable the gameObject.
			gameObject.SetActive (false);

			GameManager.Map.RemoveContentAt(TileContent.Wall, modelPosition);

			//Random choice whether or not to drop food.
			if (Random.value <= chanceOfFood) {
				Instantiate(foodPrefab, transform.position, Quaternion.identity);
				GameManager.Map.SetContentAt(TileContent.Food, modelPosition);
			}

		}

	}
}
