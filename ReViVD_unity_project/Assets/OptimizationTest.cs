using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OptimizationTest : MonoBehaviour {
    
    class Test {
        public Test() { Debug.Log("beep"); }
        public int a = 0;
    }

    List<Test> L;

    IReadOnlyList<Test> getList() {
        return L;
    }

	// Use this for initialization
	void Start () {
        L = new List<Test>();
        L.Add(new Test());
        getList()[0].a = 1;
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
