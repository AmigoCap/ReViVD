using UnityEngine.UI;
using UnityEngine;

namespace Revivd {
    public class Spheres : MonoBehaviour {
        public Toggle display;
        public Toggle animate;
        public Button drop;
        public InputField globalTime;
        public Button backToGlobalTime;
        public InputField animSpeed;
        public InputField radius;

        private void OnEnable() {
            display.onValueChanged.AddListener((bool isOn) => { ControlPanel.Instance.TransmitCommand<bool>(ControlPanel.Command.DisplaySpheres, isOn); });
            animate.onValueChanged.AddListener((bool isOn) => { ControlPanel.Instance.TransmitCommand<bool>(ControlPanel.Command.AnimSpheres, isOn); });
            drop.onClick.AddListener(() => {
                globalTime.gameObject.SetActive(false);
                backToGlobalTime.gameObject.SetActive(true);
                ControlPanel.Instance.TransmitCommand(ControlPanel.Command.DropSpheres);
            });
            globalTime.onValueChanged.AddListener(delegate { ControlPanel.Instance.TransmitCommand<float>(ControlPanel.Command.SetSpheresGlobalTime, Tools.ParseField_f(globalTime, 0)); });
            backToGlobalTime.onClick.AddListener(() => {
                globalTime.gameObject.SetActive(true);
                backToGlobalTime.gameObject.SetActive(false);
                ControlPanel.Instance.TransmitCommand(ControlPanel.Command.UseGlobalTime);
            });
            animSpeed.onValueChanged.AddListener(delegate { ControlPanel.Instance.TransmitCommand<float>(ControlPanel.Command.SetSpheresAnimSpeed, Tools.ParseField_f(animSpeed, 1)); });
            radius.onValueChanged.AddListener(delegate { ControlPanel.Instance.TransmitCommand<float>(ControlPanel.Command.SetSpheresRadius, Tools.ParseField_f(radius, 2)); });
        }

        private void OnDisable() {
            display.onValueChanged.RemoveAllListeners();
            animate.onValueChanged.RemoveAllListeners();
            drop.onClick.RemoveAllListeners();
            globalTime.onValueChanged.RemoveAllListeners();
            animSpeed.onValueChanged.RemoveAllListeners();
            radius.onValueChanged.RemoveAllListeners();
        }
    }
}
