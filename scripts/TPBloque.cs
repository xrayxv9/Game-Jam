using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TPBloque : MonoBehaviour
{
	int _sceneIndex;
    // Start is called before the first frame update
    void Start()
    {
        _sceneIndex = SceneManager.GetActiveScene().buildIndex;
    }

	public void ChangeSide( GameObject player )
	{
		PlayerController control = player.GetComponent<PlayerController>();
		if (_sceneIndex % 2 == 0)
			_sceneIndex++;
		else
			_sceneIndex--;
		control.loadScene(_sceneIndex);
	}
}
