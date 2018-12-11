using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScript : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}

    void PrintVect(Vector3 vect) {
        Debug.Log(vect.x.ToString() + ' ' + vect.y.ToString() + ' ' + vect.z.ToString());
    }

    // Update is called once per frame
    void Update () {
        if (Input.GetKey(KeyCode.Space)) {
            Renderer rend = GetComponent<Renderer>();
            rend.material.SetColor("_Color", Color.red);
        }
        if (Input.GetButton("Trig_Right")) {
            Renderer rend = GetComponent<Renderer>();
            rend.material.SetColor("_Color", Color.blue);
        }
        if (Input.GetButton("Trig_Left")) {
            Renderer rend = GetComponent<Renderer>();
            rend.material.SetColor("_Color", Color.yellow);
        }

        transform.Rotate(100 * Time.deltaTime, 0, 0);
    }
}
