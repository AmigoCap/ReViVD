using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Filters : MonoBehaviour {

    Transform leftHand;
    Transform rightHand;

	// Use this for initialization
	void Start () {
        leftHand = GameObject.FindWithTag("leftHand").transform;
        rightHand = GameObject.FindWithTag("rightHand").transform;
	}
	
	// Update is called once per frame
	void Update () {
	}
}
