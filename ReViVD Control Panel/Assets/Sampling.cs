using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class Sampling : MonoBehaviour
{
#pragma warning disable 0649
    [SerializeField] GameObject randomPathsLine;
    [SerializeField] GameObject standardPathsLine;
    [SerializeField] GameObject rangePathsLine;
    [SerializeField] GameObject instantsLine;
#pragma warning restore 0649

    public Toggle randomPaths;
    public Toggle allPaths;
    public Toggle allInstants;
    public InputField n_paths;
    public InputField paths_start;
    public InputField paths_end;
    public InputField paths_step;
    public InputField instants_start;
    public InputField instants_end;
    public InputField instants_step;

    void RandomSwitch(bool isOn) {
        randomPathsLine.SetActive(isOn);
        standardPathsLine.SetActive(!isOn);
        rangePathsLine.GetComponent<RectTransform>().anchoredPosition = new Vector2(isOn ? 607 : 422, rangePathsLine.GetComponent<RectTransform>().anchoredPosition.y);
    }

    void AllPathsSwitch(bool isOn) {
        randomPaths.interactable = !isOn;
        randomPathsLine.SetActive(!isOn && randomPaths.isOn);
        standardPathsLine.SetActive(!isOn && !randomPaths.isOn);
        rangePathsLine.SetActive(!isOn);
        instantsLine.GetComponent<RectTransform>().anchoredPosition = new Vector2(instantsLine.GetComponent<RectTransform>().anchoredPosition.x, isOn ? -95 : -125);
        GetComponent<RectTransform>().sizeDelta = new Vector2(GetComponent<RectTransform>().sizeDelta.x, 140 - ((isOn ? 1 : 0) + (allInstants.isOn ? 1 : 0)) * 30);
        VerticalLayoutGroup lg = ScrollbarContent.Instance.GetComponent<VerticalLayoutGroup>();
        lg.spacing = lg.spacing + 1; //Forcing update of the layout group
        lg.spacing = lg.spacing - 1;
    }

    void AllInstantsSwitch(bool isOn) {
        instantsLine.SetActive(!isOn);
        GetComponent<RectTransform>().sizeDelta = new Vector2(GetComponent<RectTransform>().sizeDelta.x, 140 - ((isOn ? 1 : 0) + (allInstants.isOn ? 1 : 0)) * 30);
        VerticalLayoutGroup lg = ScrollbarContent.Instance.GetComponent<VerticalLayoutGroup>();
        lg.spacing = lg.spacing + 1; //Forcing update of the layout group
        lg.spacing = lg.spacing - 1;
    }

    void CapPaths() {
        if (paths_end.text == "" || !Launcher.Instance.DataLoaded)
            return;
        if (Tools.ParseField_i(paths_end, Launcher.Instance.data.file_n_paths) >= Launcher.Instance.data.file_n_paths)
            paths_end.text = Launcher.Instance.data.file_n_paths.ToString();
    }

    void CapInstants() {
        if (instants_end.text == "" || !Launcher.Instance.DataLoaded)
            return;

        if (Tools.ParseField_i(instants_end, Launcher.Instance.data.file_n_instants) >= Launcher.Instance.data.file_n_instants)
            instants_end.text = Launcher.Instance.data.file_n_instants.ToString();
    }

    void OnEnable() {
        randomPaths.onValueChanged.AddListener(RandomSwitch);

        paths_end.onValueChanged.AddListener(delegate { CapPaths(); });
        instants_end.onValueChanged.AddListener(delegate { CapInstants(); });

        allPaths.onValueChanged.AddListener(AllPathsSwitch);
        allInstants.onValueChanged.AddListener(AllInstantsSwitch);
    }

    private void OnDisable() {
        randomPaths.onValueChanged.RemoveAllListeners();
        paths_end.onValueChanged.RemoveAllListeners();
        instants_end.onValueChanged.RemoveAllListeners();
    }
}
