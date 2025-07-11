using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WinFlag : MonoBehaviour
{
	private int _sceneIndex;
	public int win = 1;
    // Start is called before the first frame update
    void Start()
    {
        
        _sceneIndex = SceneManager.GetActiveScene().buildIndex;
    }

	void OnCollisionEnter2D(Collision2D collision)
	{
		if (collision.gameObject.CompareTag("Player"))
		{
			if (_sceneIndex % 2 == 0)
				_sceneIndex += win * 2;
			else
				_sceneIndex += win;
			SceneManager.LoadScene(_sceneIndex);
		}
	}

    // Update is called once per frame
    void Update()
    {
        
    }
}
