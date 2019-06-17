using UnityEngine;

namespace Revivd {

    [DisallowMultipleComponent]
    public class ColorPad : MonoBehaviour {
        float prevFingerX = 0;
        bool swiping = false;

        public float dampener = 50;
        public float centeringSpeed = 3f;

        int color;

        private void Start() {
            color = (int)SelectorManager.Instance.CurrentColor;
            GetComponent<RectTransform>().localPosition = new Vector3(2.5f - color, 0, 0);
        }

        void Update() {
            RectTransform rt = GetComponent<RectTransform>();
            if (SteamVR_ControllerManager.RightController.padTouched) {
                if (!swiping)
                    swiping = true;
                else {
                    rt.Translate(Tools.Limit(SteamVR_ControllerManager.RightController.Pad.x - prevFingerX, -2.5f - rt.localPosition.x, 2.5f - rt.localPosition.x) / dampener, 0, 0);
                }
                prevFingerX = SteamVR_ControllerManager.RightController.Pad.x;
            }
            else {
                swiping = false;
                if (Mathf.Abs(2.5f - color - rt.localPosition.x) < centeringSpeed * Time.deltaTime) {
                    rt.localPosition = new Vector3(2.5f - color, 0, 0);
                }
                else {
                    rt.localPosition = new Vector3(rt.localPosition.x + Tools.Sign(2.5f - color - rt.localPosition.x) * centeringSpeed * Time.deltaTime, 0, 0);
                }
            }

            color = Mathf.FloorToInt(3f - rt.localPosition.x);
            if (color != (int)SelectorManager.Instance.CurrentColor)
                SelectorManager.Instance.CurrentColor = (SelectorManager.ColorGroup)color;
        }
    }

}