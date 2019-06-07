using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Revivd {
    public class CreationLeftPad : MonoBehaviour {
        public GameObject invert;
        public GameObject leftscalePad;

        // Start is called before the first frame update
        void Start() {

        }

        // Update is called once per frame
        void Update() {

        }

        void SetFullOrEmptySprites(SteamVR_TrackedController sender) {

            SelectorManager sm = SelectorManager.Instance;
            if (sm.CurrentControlMode != SelectorManager.ControlMode.SelectionMode)
                return;

            Selector hs = sm.handSelectors[(int)sm.CurrentColor];

            if (hs != null && hs.isActiveAndEnabled) {
                foreach (SelectorPart p in hs.GetComponents<SelectorPart>()) {
                    if (p.name == "Cuboid") {//it works only if there is one primitive per selector
                        leftscalePad.SetActive(true);
                    }
                    else {
                        leftscalePad.SetActive(false);
                    }
                }
            }

            if (sm.InverseMode) {
                leftscalePad.SetActive(false);
                invert.SetActive(true);
                return;
            }
            else {
                invert.SetActive(false);
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