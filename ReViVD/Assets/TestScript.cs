using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestScript : MonoBehaviour {

    // Use this for initialization
    void Start() {

    }

    // Update is called once per frame
    void Update() {
        transform.Rotate(Time.deltaTime * 20, Time.deltaTime * 13, Time.deltaTime * 27);
    }

    void ChangeColor(SteamVR_TrackedController sender) {
        GetComponent<MeshRenderer>().material.color = Random.ColorHSV();
    }

    private void OnEnable() {
        SteamVR_ControllerManager.RightController.TriggerClicked += ChangeColor;
    }

    private void OnDisable() {
        SteamVR_ControllerManager.RightController.TriggerClicked -= ChangeColor;
    }   
}