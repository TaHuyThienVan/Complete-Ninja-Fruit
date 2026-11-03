using UnityEngine;

public class Singleton<T> : VanMonoBeha where T : VanMonoBeha
{

	private static T _instance = null; // T tuong trung cho ten class
	public static T instance
	{
		get
		{
			if (_instance == null)
			{
				T found = FindAnyObjectByType<T>();
				if (found != null)
					_instance = found;
				else
					new GameObject().AddComponent<T>().name = "Singleton_"+  typeof(T).ToString();
			}

			return _instance;
		}
	}

	
	protected override void Awake()
	{
		base.Awake();
		if (_instance != null && _instance != this)
		{
			Debug.LogError("Singleton already exist "+ _instance.gameObject.name);
			Destroy(this.gameObject);
		}
		else
			_instance = this.GetComponent<T>();
	}

}

