using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Analytics;
using SimpleJSON;

public class Analytics: MonoBehaviour {
	
	private static Analytics instance = null;

	void Awake() {

		if (instance == null) {
			instance = this;
			InitializeUUID();
			DontDestroyOnLoad(gameObject);
		} else if (instance != this) {
			Destroy(gameObject);
		}

	}

	private string uuid;

	private void InitializeUUID() {

		string savedUUID = PlayerPrefs.GetString("analytics_uuid");

		Guid guid = new Guid();
		bool hasGuid = false;

		try { guid = new Guid(savedUUID); hasGuid = true; }
		catch (Exception e) { hasGuid = false; }

		if (!hasGuid) { guid = Guid.NewGuid(); }

		uuid = guid.ToString().ToUpper();

		if (savedUUID != uuid) {
			PlayerPrefs.SetString("analytics_uuid", uuid);
			PlayerPrefs.Save();
		}

	}

	private int currentLevel;
	private float currentLevelStartTime;
	private float firstLevelStartTime;

	public static void ReportLevelStart(int level, bool isFirstLevel) {

		instance.currentLevel = level;
		instance.currentLevelStartTime = Time.time;
		if (isFirstLevel) { instance.firstLevelStartTime = Time.time; }

		SendEvent("levelStart", new Dictionary<string, object> {
			{ "level", level }
		});

	}

	public static void ReportLevelSuccess(int remainigFood) {

		float levelTime = Time.time - instance.currentLevelStartTime;
		float totalTime = Time.time - instance.firstLevelStartTime;

		SendEvent("levelSuccess", new Dictionary<string, object> {
			{ "level", instance.currentLevel },
			{ "remainigFood", remainigFood },
			{ "levelTime", levelTime },
			{ "totalTime", totalTime }
		});

	}

	public static void ReportLevelFailure() {

		float levelTime = Time.time - instance.currentLevelStartTime;
		float totalTime = Time.time - instance.firstLevelStartTime;

		SendEvent("levelFailure", new Dictionary<string, object> {
			{ "level", instance.currentLevel },
			{ "levelTime", levelTime },
			{ "totalTime", totalTime }
		});

	}

	public static void ReportFoodPickup(string foodType, int newFoodCount) {
		
		float levelTime = Time.time - instance.currentLevelStartTime;

		SendEvent("pickup", new Dictionary<string, object> {
			{ "level", instance.currentLevel },
			{ "levelTimeSoFar", levelTime },
			{ "newFoodCount", newFoodCount },
			{ "foodType", foodType }
		});

	}

	public static void ReportTakeDamage(int remainigFood) {

		float levelTime = Time.time - instance.currentLevelStartTime;

		SendEvent("takeDamage", new Dictionary<string, object> {
			{ "level", instance.currentLevel },
			{ "levelTimeSoFar", levelTime },
			{ "remainigFood", remainigFood }
		});

	}

	// send to unity analytics and to the private server
	private static void SendEvent(string eventName, Dictionary<string, object> eventData) {

		UnityEngine.Analytics.Analytics.CustomEvent(eventName, eventData);
		instance.SendToPrivateServer(eventName, eventData);

	}

	private void SendToPrivateServer(string eventName, Dictionary<string, object> eventData) {

		WWWForm postData = new WWWForm();

		postData.AddField("user_uuid", uuid);

		string timestamp = DateTime.UtcNow.ToString("u");
		postData.AddField("event_timestamp", timestamp);

		JSONObject data = new JSONObject();

		foreach (KeyValuePair<string, object> entry in eventData) {

			object valueObject = entry.Value;
			JSONNode node = null;

			if (valueObject is bool) { node = new JSONBool((bool)valueObject); }
			else if (valueObject is int) { node = new JSONNumber(valueObject.ToString()); }
			else if (valueObject is float) { node = new JSONNumber((float)valueObject); }
			else if (valueObject is string) { node = new JSONString((string)valueObject); }
			else { Debug.LogError("Unsupported type in analytics."); }

			if (node != null) { data.Add(entry.Key, node); }

		}

		postData.AddField("event_name", eventName);
		postData.AddField("event_data", data.ToString());

		WWW httpRequest = new WWW("https://thejv.club/roguelike-analytics/", postData);

		StartCoroutine(SendRequest(httpRequest));

	}

	private IEnumerator SendRequest(WWW httpRequest) {
		yield return httpRequest;
	}

}
