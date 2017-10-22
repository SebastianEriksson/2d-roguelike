using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FoodLoss : MonoBehaviour {

	public float duration;
	public float distance;
	public Text text;

	float alpha = 1;

	void Update() {

		Vector3 pos = transform.position;
		pos.y += Time.deltaTime * distance;
		transform.position = pos;

		alpha -= Time.deltaTime / duration;

		if (alpha < float.Epsilon) {
			Destroy(gameObject);
			return;
		}

		Color color = text.color;
		color.a = alpha;
		text.color = color;// = new Color(1, 1, 1, alpha);

	}

}
