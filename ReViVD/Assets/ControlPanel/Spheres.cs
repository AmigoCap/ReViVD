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
            display.onValueChanged.AddListener((bool isOn) => {
                if (Visualization.Instance.Loaded) {
                    Visualization.Instance.displayTimeSpheres = isOn;
                    animate.interactable = isOn;
                    if (!isOn)
                        animate.isOn = false;
                    drop.interactable = isOn;
                }
            });

            animate.onValueChanged.AddListener((bool isOn) => {
                if (Visualization.Instance.Loaded)
                    Visualization.Instance.doTimeSphereAnimation = isOn;
            });

            drop.onClick.AddListener(() => {
                if (Visualization.Instance.Loaded) {
                    globalTime.gameObject.SetActive(false);
                    backToGlobalTime.gameObject.SetActive(true);
                    Visualization.Instance.useGlobalTime = false;
                    Visualization.Instance.doSphereDrop = true;
                }
            });

            globalTime.onValueChanged.AddListener(delegate {
                if (Visualization.Instance.Loaded && globalTime.isFocused) //Avoid setting the visualization's time if the visualization itself changed this field
                    Visualization.Instance.globalTime = Tools.ParseField_f(globalTime, 0);
            });

            backToGlobalTime.onClick.AddListener(() => {
                if (Visualization.Instance.Loaded) {
                    globalTime.gameObject.SetActive(true);
                    backToGlobalTime.gameObject.SetActive(false);
                    Visualization.Instance.useGlobalTime = true;
                }
            });

            animSpeed.onValueChanged.AddListener(delegate {
                if (Visualization.Instance.Loaded)
                    Visualization.Instance.timeSphereAnimationSpeed = Tools.ParseField_f(animSpeed, 1);
            });

            radius.onValueChanged.AddListener(delegate {
                if (Visualization.Instance.Loaded)
                    Visualization.Instance.timeSphereRadius = Tools.ParseField_f(radius, 2);
            });
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
