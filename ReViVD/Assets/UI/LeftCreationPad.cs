using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Revivd {
    public class LeftCreationPad : MonoBehaviour {
        public GameObject invert;
        public GameObject creationPad;
        public GameObject creationReset;

        float prevFingerY = 0;
        bool swiping_reset = false;

        public float dampener = 45;
        public float pullSpeed = 0.2f;
        public float pullThreshold = 0.7f;
        public float operationThreshold = 0.2f;
        public float pullMaxStartPos = 0.45f;

        void DoPadClickAction(SteamVR_TrackedController sender) {
            if (GetComponent<RectTransform>().localPosition.y > pullThreshold) {
                SelectorManager sm = SelectorManager.Instance;
                Selector hs = sm.handSelectors[(int)sm.CurrentColor];

                if (hs != null && hs.isActiveAndEnabled) {
                    foreach (SelectorPart p in hs.GetComponents<SelectorPart>()) {
                        p.ResetScale();
                    }
                }
            }
        }

        // Update is called once per frame
        void Update() {
            SelectorManager sm = SelectorManager.Instance;
            Selector hs = sm.handSelectors[(int)sm.CurrentColor];

            if (hs != null && hs.isActiveAndEnabled) {
                foreach (SelectorPart p in hs.GetComponents<SelectorPart>()) {
                    if (p.name == "Cuboid" && !sm.InverseMode && !swiping_reset) {//it works only if there is one primitive per selector
                        creationPad.SetActive(true);
                    }
                    else {
                        creationPad.SetActive(false);
                    }
                }
            }

            RectTransform rt = GetComponent<RectTransform>();
            if (SteamVR_ControllerManager.LeftController.padTouched) {
                if (swiping_reset) {
                    rt.Translate(0, Tools.Limit(SteamVR_ControllerManager.LeftController.Pad.y - prevFingerY, -rt.localPosition.y, 0.9f - rt.localPosition.y) / dampener, 0);
                    if (Mathf.Abs(GetComponent<RectTransform>().localPosition.y) > pullThreshold)
                        SteamVR_ControllerManager.LeftController.Vibrate();
                }
                else {
                    if (SteamVR_ControllerManager.LeftController.Pad.y < -pullMaxStartPos)
                        swiping_reset = true;
                }
                prevFingerY = SteamVR_ControllerManager.LeftController.Pad.y;
            }
            else {
                swiping_reset = false;
                float pull = Tools.MaxAbs(-rt.localPosition.y, pullSpeed);
                if (Mathf.Abs(pull) < pullSpeed / 100)
                    pull = 0;
                rt.Translate(0, pull * Time.deltaTime, 0);
            }
        }


    void SetFullOrEmptySprites(SteamVR_TrackedController sender) {

            SelectorManager sm = SelectorManager.Instance;

            if (sm.InverseMode) {
                Selector hs = sm.handSelectors[(int)sm.CurrentColor];
                if (hs != null && hs.isActiveAndEnabled) {
                    foreach (SelectorPart p in hs.GetComponents<SelectorPart>()) {
                        if (p.name == "Cuboid") {//it works only if there is one primitive per selector
                            creationPad.SetActive(false);
                            invert.SetActive(false);
                            creationReset.SetActive(true);
                            return;
                        }
                        else {
                            creationPad.SetActive(false);
                            invert.SetActive(true);
                            creationReset.SetActive(false);
                            return;
                        }
                    }
                }
            }
            else {
                creationPad.SetActive(true);
                invert.SetActive(false);
                creationReset.SetActive(false);
            }
        }

        private void OnEnable() {
            SteamVR_ControllerManager.LeftController.PadClicked += DoPadClickAction;
            SteamVR_ControllerManager.LeftController.TriggerClicked += SetFullOrEmptySprites;
            SteamVR_ControllerManager.LeftController.TriggerUnclicked += SetFullOrEmptySprites;

            SetFullOrEmptySprites(SteamVR_ControllerManager.RightController);
        }

        private void OnDisable() {
            if (SteamVR_ControllerManager.LeftController != null) {
                SteamVR_ControllerManager.LeftController.PadClicked -= DoPadClickAction;
                SteamVR_ControllerManager.LeftController.TriggerClicked -= SetFullOrEmptySprites;
                SteamVR_ControllerManager.LeftController.TriggerUnclicked -= SetFullOrEmptySprites;
            }
        }

    }

}