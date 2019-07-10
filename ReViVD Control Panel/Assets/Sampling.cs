using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class Sampling : MonoBehaviour
{
#pragma warning disable 0649
    [SerializeField] GameObject randomLine;
    [SerializeField] GameObject standardLine;
    [SerializeField] RectTransform rangeLineTransform;
#pragma warning restore 0649

    public Toggle randomPaths;
    public InputField n_paths;
    public InputField paths_start;
    public InputField paths_end;
    public InputField paths_step;
    public InputField instants_start;
    public InputField instants_end;
    public InputField instants_step;

    float xPosRandom;
    float xPosStandard;

    void Switch() {
        randomLine.SetActive(randomPaths.isOn);
        standardLine.SetActive(!randomPaths.isOn);
        rangeLineTransform.localPosition = new Vector3(randomPaths.isOn ? xPosRandom : xPosStandard, rangeLineTransform.localPosition.y, 0);
    }

    void CapInput(InputField field, int max) {
        if (field.text == "")
            return;
        if (int.Parse(field.text) > max)
            field.text = max.ToString();
    }

    void OnEnable() {
        xPosRandom = rangeLineTransform.localPosition.x;
        xPosStandard = xPosRandom - 185;
        randomPaths.onValueChanged.AddListener(delegate { Switch(); });

        paths_end.onValueChanged.AddListener(delegate { CapInput(paths_end, 1500); });
        instants_end.onValueChanged.AddListener(delegate { CapInput(instants_end, 1000); });
    }

    private void OnDisable() {
        randomPaths.onValueChanged.RemoveAllListeners();
        paths_end.onValueChanged.RemoveAllListeners();
        instants_end.onValueChanged.RemoveAllListeners();
    }
}
