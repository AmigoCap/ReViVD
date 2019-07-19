using UnityEngine;
using UnityEngine.UI;

public static class ExtensionMethods { //Fix for unity bug
    public static void SetSprite(this Image image, Sprite sprite) {
        if (image.sprite != sprite) {
            image.sprite = null;
            if (sprite != null) {
                image.sprite = sprite;
            }
        }
    }
}

namespace Revivd {

    [DisallowMultipleComponent]
    public class OperationPad : MonoBehaviour {
        float prevFingerY = 0;
        bool swiping_reset = false;
        bool swiping_logic = false;

        public float dampener = 45;
        public float pullSpeed = 3f;
        public float pullThreshold = 0.7f;
        public float operationThreshold = 0.2f;
        public float pullMaxStartPos = 0.45f;

        public GameObject logicPulldown;
        public Sprite andPulldown;
        public Sprite orPulldown;

        public GameObject centerpiece;
        public GameObject squares;

        public Sprite emptySquare;
        public Sprite fullSquare;
        public Sprite andSign;
        public Sprite orSign;

        public GameObject resetPullup;
        public Sprite reset;
        public Sprite hardReset;

        public GameObject invert;

        void DoPadClickAction(SteamVR_TrackedController sender) {
            if (SelectorManager.Instance.CurrentControlMode != SelectorManager.ControlMode.SelectionMode)
                return;

            if (GetComponent<RectTransform>().localPosition.y > pullThreshold)
                ResetDisplayedPaths();
            else if (GetComponent<RectTransform>().localPosition.y < -pullThreshold)
                ToggleLogicOperation();
            else if (GetComponent<RectTransform>().localPosition.y > -operationThreshold && GetComponent<RectTransform>().localPosition.y < operationThreshold)
                SelectorManager.Instance.DoLogicOperation();

        }

        void ResetDisplayedPaths() {
            foreach (Path p in Visualization.Instance.paths) {
                foreach (Atom a in p.atoms) {
                    a.ShouldDisplayBecauseSelected = true;
                }
            }
            if (SelectorManager.Instance.InverseMode) { //Hard reset
                for (int i = 0; i < SelectorManager.colors.Length; i++) {
                    SelectorManager.Instance.ClearSelected((SelectorManager.ColorGroup)i);
                }
                Logger.Instance?.LogEvent("HRESET");
            }
            else
                Logger.Instance?.LogEvent("RESET");
        }

        void ToggleLogicOperation() {
            if (SelectorManager.Instance.operationMode == SelectorManager.LogicMode.OR) {
                SelectorManager.Instance.operationMode = SelectorManager.LogicMode.AND;
                logicPulldown.GetComponent<Image>().SetSprite(andPulldown);
                centerpiece.GetComponent<Image>().SetSprite(andSign);

                Logger.Instance?.LogEvent("CHOPMODE,AND");
                SteamVR_ControllerManager.LeftController.Vibrate();
            }
            else {
                SelectorManager.Instance.operationMode = SelectorManager.LogicMode.OR;
                logicPulldown.GetComponent<Image>().SetSprite(orPulldown);
                centerpiece.GetComponent<Image>().SetSprite(orSign);

                Logger.Instance?.LogEvent("CHOPMODE,OR");
                SteamVR_ControllerManager.LeftController.Vibrate();
                StartCoroutine(SteamVR_ControllerManager.LeftController.VibrateAfter(0.1f));
            }
        }

        void SetFullOrEmptySprites(SteamVR_TrackedController sender) {
            if (SelectorManager.Instance.CurrentControlMode != SelectorManager.ControlMode.SelectionMode)
                return;

            SelectorManager sm = SelectorManager.Instance;

            if (sm.InverseMode) {
                invert.SetActive(true);
                resetPullup.GetComponent<Image>().SetSprite(hardReset);
                return;
            }
            else {
                invert.SetActive(false);
                resetPullup.GetComponent<Image>().SetSprite(reset);
            }

            if (sm.operationMode == SelectorManager.LogicMode.OR)
                centerpiece.GetComponent<Image>().SetSprite(orSign);
            else
                centerpiece.GetComponent<Image>().SetSprite(andSign);

            Image[] squareImages = squares.GetComponentsInChildren<Image>(false);
            for (int i = 0; i < System.Math.Min(SelectorManager.colors.Length, squareImages.Length); i++) {
                if (sm.operatingColors.Contains((SelectorManager.ColorGroup)i))
                    squareImages[i].SetSprite(fullSquare);
                else
                    squareImages[i].SetSprite(emptySquare);
            }
        }

        void ToggleCurrentColorAsOperative(SteamVR_TrackedController sender) {
            if (SelectorManager.Instance.CurrentControlMode != SelectorManager.ControlMode.SelectionMode)
                return;

            SelectorManager sm = SelectorManager.Instance;
            if (sm.operatingColors.Contains(sm.CurrentColor)) {
                sm.operatingColors.Remove(sm.CurrentColor);

                Logger.Instance?.LogEvent("CHOPCOL,-" + Logger.colorString[(int)sm.CurrentColor]);
                SteamVR_ControllerManager.LeftController.Vibrate();
                StartCoroutine(SteamVR_ControllerManager.LeftController.VibrateAfter(0.1f));
            }
            else {
                sm.operatingColors.Add(sm.CurrentColor);

                Logger.Instance?.LogEvent("CHOPCOL,+" + Logger.colorString[(int)sm.CurrentColor]);
                SteamVR_ControllerManager.LeftController.Vibrate();
            }

            SetFullOrEmptySprites(sender);
        }

        void Update() {

            RectTransform rt = GetComponent<RectTransform>();

            if (SteamVR_ControllerManager.LeftController.padTouched) {
                if (swiping_reset) {
                    rt.Translate(0, Tools.Limit(SteamVR_ControllerManager.LeftController.Pad.y - prevFingerY, -rt.localPosition.y, 0.9f - rt.localPosition.y) / dampener, 0);
                    if (Mathf.Abs(GetComponent<RectTransform>().localPosition.y) > pullThreshold)
                        SteamVR_ControllerManager.LeftController.Vibrate();
                }
                if (swiping_logic) {
                    rt.Translate(0, Tools.Limit(SteamVR_ControllerManager.LeftController.Pad.y - prevFingerY, -0.9f - rt.localPosition.y, -rt.localPosition.y) / dampener, 0);
                }

                if (!swiping_reset && !swiping_logic) {
                    if (SteamVR_ControllerManager.LeftController.Pad.y < -pullMaxStartPos)
                        swiping_reset = true;
                    else if (SteamVR_ControllerManager.LeftController.Pad.y > pullMaxStartPos)
                        swiping_logic = true;
                }

                prevFingerY = SteamVR_ControllerManager.LeftController.Pad.y;
            }
            else {
                swiping_reset = false;
                swiping_logic = false;
                if (Mathf.Abs(-rt.localPosition.y) < pullSpeed * Time.deltaTime) {
                    rt.localPosition = Vector3.zero;
                }
                else {
                    rt.localPosition = new Vector3(0, rt.localPosition.y + Tools.Sign(-rt.localPosition.y) * pullSpeed * Time.deltaTime, 0);
                }
            }

        }

        private void OnEnable() {
            SteamVR_ControllerManager.LeftController.PadClicked += DoPadClickAction;
            SteamVR_ControllerManager.LeftController.TriggerClicked += SetFullOrEmptySprites;
            SteamVR_ControllerManager.LeftController.TriggerUnclicked += SetFullOrEmptySprites;
            SteamVR_ControllerManager.RightController.PadClicked += ToggleCurrentColorAsOperative;

            SetFullOrEmptySprites(SteamVR_ControllerManager.RightController);
        }

        private void OnDisable() {
            if (SteamVR_ControllerManager.LeftController != null) {
                SteamVR_ControllerManager.LeftController.PadClicked -= DoPadClickAction;
                SteamVR_ControllerManager.LeftController.TriggerClicked -= SetFullOrEmptySprites;
                SteamVR_ControllerManager.LeftController.TriggerUnclicked -= SetFullOrEmptySprites;
            }

            if (SteamVR_ControllerManager.RightController != null)
                SteamVR_ControllerManager.RightController.PadClicked -= ToggleCurrentColorAsOperative;
        }
    }

}