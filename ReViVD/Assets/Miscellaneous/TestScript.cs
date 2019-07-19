using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR;

namespace Revivd {

    public class TestScript : MonoBehaviour {

        // Update is called once per frame
        void Update() {
            transform.Rotate(Time.deltaTime * 20, Time.deltaTime * 13, Time.deltaTime * 27);
        }

        void ChangeColor(SteamVR_TrackedController sender) {
            GetComponent<MeshRenderer>().material.color = Random.ColorHSV();
        }

        private void Start() {
            XRSettings.eyeTextureResolutionScale = 1;
            XRSettings.gameViewRenderMode = GameViewRenderMode.None;
            XRSettings.renderViewportScale = 1;
        }

        private void OnEnable() {
            SteamVR_ControllerManager.LeftController.TriggerClicked += ChangeColor;
        }

        private void OnDisable() {
            if (SteamVR_ControllerManager.LeftController != null)
                SteamVR_ControllerManager.LeftController.TriggerClicked -= ChangeColor;
        }
    }
}

