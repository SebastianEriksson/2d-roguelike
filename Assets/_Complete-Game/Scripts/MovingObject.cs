using UnityEngine;
using System.Collections;

namespace Completed
{
	//The abstract keyword enables you to create classes and class members that are incomplete and must be implemented in a derived class.
	public abstract class MovingObject : BlockingObject
	{
		public float moveTime = 0.1f;			//Time it will take object to move, in seconds.
		public LayerMask blockingLayer;			//Layer on which collision will be checked.
		
		
		private BoxCollider2D boxCollider; 		//The BoxCollider2D component attached to this object.
		private Rigidbody2D rb2D;				//The Rigidbody2D component attached to this object.
		private float inverseMoveTime;			//Used to make movement more efficient.

		public float horizontalScaleMultiplier = 1;

		//Protected, virtual functions can be overridden by inheriting classes.
		protected virtual void Start ()
		{
			//Get a component reference to this object's BoxCollider2D
			boxCollider = GetComponent <BoxCollider2D> ();
			
			//Get a component reference to this object's Rigidbody2D
			rb2D = GetComponent <Rigidbody2D> ();
			
			//By storing the reciprocal of the move time we can use it by multiplying instead of dividing, this is more efficient.
			inverseMoveTime = 1f / moveTime;
		}
		
		
		//Move returns true if it is able to move and false if not. 
		//Move takes parameters for x direction, y direction.
		protected bool Move (int xDir, int yDir)
		{
			if (xDir != 0) {
				transform.localScale = new Vector3(xDir * horizontalScaleMultiplier, 1, 1);
			}

			Point nextPosition = modelPosition;
			nextPosition.x += xDir;
			nextPosition.y += yDir;

			MapGenerator map = GameManager.Map;

			if (!map.GetContentAt(nextPosition).CanMoveTo()) { return false; }

			map.RemoveContentAt(ContentType, modelPosition);
			map.SetContentAt(ContentType, nextPosition);
			modelPosition = nextPosition;

			StartCoroutine(SmoothMovement(nextPosition.Vector));

			return true;
		}

		abstract protected TileContent ContentType { get; }
		
		
		//Co-routine for moving units from one space to next, takes a parameter end to specify where to move to.
		protected IEnumerator SmoothMovement (Vector3 end)
		{
			//Calculate the remaining distance to move based on the square magnitude of the difference between current position and end parameter. 
			//Square magnitude is used instead of magnitude because it's computationally cheaper.
			float sqrRemainingDistance = (transform.position - end).sqrMagnitude;
			
			//While that distance is greater than a very small amount (Epsilon, almost zero):
			while(sqrRemainingDistance > float.Epsilon)
			{
				//Find a new position proportionally closer to the end, based on the moveTime
				Vector3 newPostion = Vector3.MoveTowards(rb2D.position, end, inverseMoveTime * Time.deltaTime);
				
				//Call MovePosition on attached Rigidbody2D and move it to the calculated position.
				rb2D.MovePosition (newPostion);
				
				//Recalculate the remaining distance after moving.
				sqrRemainingDistance = (transform.position - end).sqrMagnitude;
				
				//Return and loop until sqrRemainingDistance is close enough to zero to end the function
				yield return null;
			}

			SmoothMovementDone();
		}

		// override point
		protected virtual void SmoothMovementDone() {
		}
		
		
		//The virtual keyword means AttemptMove can be overridden by inheriting classes using the override keyword.
		//AttemptMove takes a generic parameter T to specify the type of component we expect our unit to interact with if blocked (Player for Enemies, Wall for Player).
		//Returns whether or not the object was able to move.
		protected virtual bool AttemptMove <T> (int xDir, int yDir, T hint)
			where T : BlockingObject
		{
			//Set canMove to true if Move was successful, false if failed.
			bool canMove = Move (xDir, yDir);

			if (canMove) { return true; }

			Point hitPoint = new Point(modelPosition.x + xDir, modelPosition.y + yDir);

			if (hint != null) {
				Point hintPos = hint.modelPosition;
				if (hintPos.x == hitPoint.x && hintPos.y == hitPoint.y) {
					OnCantMove(hint);
					return false;
				}
			}

			RaycastHit2D[] hits = Physics2D.BoxCastAll(modelPosition.Vector + new Vector3(-2, -2, 0), new Vector2(5, 5), 0, new Vector2());

			T component = null;

			for (int i = 0; i < hits.Length; i += 1) {
				component = hits[i].transform.GetComponent<T>();
				if (component == null) { continue; }
				if (component.modelPosition.x != hitPoint.x) { continue; }
				if (component.modelPosition.y != hitPoint.y) { continue; }
				OnCantMove(component);
				break;
			}

			return false;

		}
		
		
		//The abstract modifier indicates that the thing being modified has a missing or incomplete implementation.
		//OnCantMove will be overriden by functions in the inheriting classes.
		protected abstract void OnCantMove <T> (T component)
			where T : Component;
	}
}
