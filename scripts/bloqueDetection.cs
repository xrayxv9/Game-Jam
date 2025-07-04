using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bloqueDetection : MonoBehaviour
{
    // Start is called before the first frame update
	private Rigidbody2D _rb;
	bool _isChangeable = true;
	public int _keyToChange;

	void Start()
    {
		_rb = GetComponent<Rigidbody2D>();    
	}

    // Update is called once per frame
	void OnCollisionEnter2D(Collision2D collision)
	{
		if (collision.gameObject.CompareTag("Player"))
		{
			PlayerController playerScript = collision.gameObject.GetComponent<PlayerController>();
			if (playerScript == null)
				Debug.LogWarning("pas de script player");
			else if (_isChangeable)
			{
				playerScript.ChangeKeyBind(_keyToChange);
				_isChangeable = false;
			}
			else _isChangeable = true;
		}
	}
}
