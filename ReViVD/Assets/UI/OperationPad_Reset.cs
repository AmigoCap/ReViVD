using UnityEngine;

namespace Revivd {

    [DisallowMultipleComponent]
    public class OperationPad_Reset : MonoBehaviour {
        float prevFingerY = 0;
        bool swiping = false;

        public float dampener = 50;
        public float pullupSpeed = 0.2f;

        void ResetDisplayedPaths(SteamVR_TrackedController sender) {
            if (GetComponent<RectTransform>().localPosition.y < 0.65f)
                return;
            foreach (Path p in Visualization.Instance.PathsAsBase) {
                foreach (Atom a in p.AtomsAsBase) {
                    a.ShouldDisplay = true;
                }
            }
        }

        void Update() {
            RectTransform rt = GetComponent<RectTransform>();

            if (SteamVR_ControllerManager.RightController.padTouched) {
                if (!swiping && SteamVR_ControllerManager.RightController.Pad.y < -0.5f)
                    swiping = true;
                else if (swiping) {
                    rt.Translate(0, Tools.Limit(SteamVR_ControllerManager.RightController.Pad.y - prevFingerY, -rt.localPosition.y, 0.85f - rt.localPosition.y) / dampener, 0);
                }
                prevFingerY = SteamVR_ControllerManager.RightController.Pad.y;
            }
            else {
                swiping = false;
                float pullup = Tools.MaxAbs(-rt.localPosition.y, pullupSpeed);
                if (Mathf.Abs(pullup) < pullupSpeed / 10)
                    pullup = 0;
                rt.Translate(0, pullup * Time.deltaTime, 0);
            }
        }

        private void OnEnable() {
            SteamVR_ControllerManager.RightController.PadClicked += ResetDisplayedPaths;
        }

        private void OnDisable() {
            if (SteamVR_ControllerManager.RightController != null)
                SteamVR_ControllerManager.RightController.PadClicked -= ResetDisplayedPaths;
        }
    }

}