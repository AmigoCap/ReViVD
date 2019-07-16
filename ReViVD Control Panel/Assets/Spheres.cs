using UnityEngine.UI;
using UnityEngine;

public class Spheres : MonoBehaviour
{
    public Toggle display;
    public Toggle animate;
    public Button drop;
    public InputField globalTime;
    public Button backToGlobalTime;
    public InputField animSpeed;
    public InputField radius;

    private void OnEnable() {
        display.onValueChanged.AddListener((bool isOn) => { Launcher.Instance.TransmitCommand<bool>(Launcher.Command.DisplaySpheres, isOn); });
        animate.onValueChanged.AddListener((bool isOn) => { Launcher.Instance.TransmitCommand<bool>(Launcher.Command.AnimSpheres, isOn); });
        drop.onClick.AddListener(() => {
            globalTime.gameObject.SetActive(false);
            backToGlobalTime.gameObject.SetActive(true);
            Launcher.Instance.TransmitCommand(Launcher.Command.DropSpheres);
        });
        globalTime.onValueChanged.AddListener(delegate { Launcher.Instance.TransmitCommand<float>(Launcher.Command.SetSpheresGlobalTime, Tools.ParseField_f(globalTime, 0)); });
        backToGlobalTime.onClick.AddListener(() => {
            globalTime.gameObject.SetActive(true);
            backToGlobalTime.gameObject.SetActive(false);
            Launcher.Instance.TransmitCommand(Launcher.Command.UseGlobalTime);
        });
        animSpeed.onValueChanged.AddListener(delegate { Launcher.Instance.TransmitCommand<float>(Launcher.Command.SetSpheresAnimSpeed, Tools.ParseField_f(animSpeed, 1)); });
        radius.onValueChanged.AddListener(delegate { Launcher.Instance.TransmitCommand<float>(Launcher.Command.SetSpheresRadius, Tools.ParseField_f(radius, 2)); });
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
