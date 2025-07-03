using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PaperRotate : MonoBehaviour
{
    // Start is called before the first frame update
    

    public float launchSpeed = 0.5f;
    
    private Rigidbody2D _rb;

    void Awake()
    {
        _rb = this.GetComponent<Rigidbody2D>();
    }
    
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        this.transform.Rotate(Vector3.forward, launchSpeed * 400);
        this._rb.MovePosition(Vector2.left + this._rb.position);
    }
}
