using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Revivd {
    public class RightCreationPad : MonoBehaviour {
        public GameObject creationPad;
        public GameObject creationReset;

        // Start is called before the first frame update
        void Start() {

        }

        // Update is called once per frame
        void Update() {

        }

        void SetFullOrEmptySprites(SteamVR_TrackedController sender) {

            SelectorManager sm = SelectorManager.Instance;
            
            if (sm.InverseMode) {
                creationPad.SetActive(false);
                creationReset.SetActive(true);
                return;
            }
            else {
                creationPad.SetActive(true);
                creationReset.SetActive(false);
            }
        }

        private void OnEnable() {
            SteamVR_ControllerManager.LeftController.TriggerClicked += SetFullOrEmptySprites;
            SteamVR_ControllerManager.LeftController.TriggerUnclicked += SetFullOrEmptySprites;

            SetFullOrEmptySprites(SteamVR_ControllerManager.RightController);
        }

        private void OnDisable() {
            if (SteamVR_ControllerManager.LeftController != null) {
                SteamVR_ControllerManager.LeftController.TriggerClicked -= SetFullOrEmptySprites;
                SteamVR_ControllerManager.LeftController.TriggerUnclicked -= SetFullOrEmptySprites;
            }
        }

    }

}