using UnityEngine;
using UnityEngine.UI;

namespace Revivd {

    [DisallowMultipleComponent]
    public class OperationPad_Rosace : MonoBehaviour {

        public Sprite emptySquare;
        public Sprite fullSquare;
        public Sprite emptyHexagon;
        public Sprite fullHexagon;

        void SetFullOrEmptySprites(SteamVR_TrackedController sender) {
            SelectorManager sm = SelectorManager.Instance;
            Image[] images = GetComponentsInChildren<Image>(false);
            images[0].sprite = sm.InverseMode ? fullHexagon : emptyHexagon;
            for (int i = 0; i < System.Math.Min(SelectorManager.colors.Length, images.Length - 1); i++) {
                if (sm.operatingColors.Contains((SelectorManager.ColorGroup)i))
                    images[i+1].sprite = sm.InverseMode ? emptySquare : fullSquare;
                else
                    images[i+1].sprite = sm.InverseMode ? fullSquare : emptySquare;
            }
        }

        void ToggleCurrentColorAsOperative(SteamVR_TrackedController sender) {
            SelectorManager sm = SelectorManager.Instance;
            if (sm.operatingColors.Contains(sm.CurrentColor)) {
                sm.operatingColors.Remove(sm.CurrentColor);
            }
            else {
                sm.operatingColors.Add(sm.CurrentColor);
            }
            SetFullOrEmptySprites(sender);
        }

        private void OnEnable() {
            SteamVR_ControllerManager.LeftController.TriggerClicked += SetFullOrEmptySprites;
            SteamVR_ControllerManager.LeftController.TriggerUnclicked += SetFullOrEmptySprites;
            SteamVR_ControllerManager.LeftController.PadClicked += ToggleCurrentColorAsOperative;

            SetFullOrEmptySprites(SteamVR_ControllerManager.RightController);
        }

        private void OnDisable() {
            if (SteamVR_ControllerManager.LeftController != null) {
                SteamVR_ControllerManager.LeftController.TriggerClicked -= SetFullOrEmptySprites;
                SteamVR_ControllerManager.LeftController.TriggerUnclicked -= SetFullOrEmptySprites;
                SteamVR_ControllerManager.LeftController.PadClicked -= ToggleCurrentColorAsOperative;
            }
        }
    }

}