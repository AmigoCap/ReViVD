using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Revivd {
    public class LeftCreationPad : MonoBehaviour {
        public GameObject creationPad;
        public GameObject creationReset;

        // Update is called once per frame
        void Update() {
            SelectorManager sm = SelectorManager.Instance;
            Selector hs = sm.handSelectors[(int)sm.CurrentColor];

            if (hs != null && hs.isActiveAndEnabled) {
                foreach (SelectorPart p in hs.GetComponents<SelectorPart>()) {
                    if (!sm.InverseMode) {//this works only if there is one primitive per selector
                        creationPad.SetActive(true);
                    }
                    else {
                        creationPad.SetActive(false);
                    }
                }
            }
        }

        void SetActiveSprite(SteamVR_TrackedController sender) {
            if (SelectorManager.Instance.InverseMode) {
                creationReset.SetActive(true);
            }
            else {
                creationReset.SetActive(false);
            }
        }

        private void OnEnable() {
            SteamVR_ControllerManager.LeftController.TriggerClicked += SetActiveSprite;
            SteamVR_ControllerManager.LeftController.TriggerUnclicked += SetActiveSprite;

            SetActiveSprite(SteamVR_ControllerManager.RightController);
        }

        private void OnDisable() {
            if (SteamVR_ControllerManager.LeftController != null) {
                SteamVR_ControllerManager.LeftController.TriggerClicked -= SetActiveSprite;
                SteamVR_ControllerManager.LeftController.TriggerUnclicked -= SetActiveSprite;
            }
        }

    }
}