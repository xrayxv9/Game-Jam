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
		// float time = control._time;
		// Transform position = player.transform;
		if (_sceneIndex % 2 == 0)
			_sceneIndex++;
		else
			_sceneIndex--;
		control.loadScene(_sceneIndex);
		// SceneManager.LoadScene(_sceneIndex);
		// Scene newScene = SceneManager.GetActiveScene();
		// GameObject[] roots = newScene.GetRootGameObjects();
		// GameObject level = null;
		//
		// foreach (var root in roots)
		// {
		// 	if (root.name == "level")
		// 	{
		// 		level = root;
		// 		break;
		// 	}
		// }
		// Transform newPlayer = level.transform.Find("Player");
		// PlayerController playerScript = newPlayer.GetComponent<PlayerController>();
		// playerScript._time = time;
		// player.transform.position = new Vector2(playerScript._gm.position.x, playerScript._gm.position.y + 1.7f);
	}
}
