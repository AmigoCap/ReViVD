using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class Sampling : MonoBehaviour
{
    public Toggle toggle;
    public GameObject randomLine;
    public GameObject standardLine;
    public RectTransform rangeLineTransform;

    public InputField paths_end;
    public InputField instants_end;

    float xPosRandom;
    float xPosStandard;

    void Switch() {
        randomLine.SetActive(toggle.isOn);
        standardLine.SetActive(!toggle.isOn);
        rangeLineTransform.localPosition = new Vector3(toggle.isOn ? xPosRandom : xPosStandard, rangeLineTransform.localPosition.y, 0);
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
        toggle.onValueChanged.AddListener(delegate { Switch(); });

        paths_end.onValueChanged.AddListener(delegate { CapInput(paths_end, 1500); });
        instants_end.onValueChanged.AddListener(delegate { CapInput(instants_end, 1000); });
    }

    private void OnDisable() {
        toggle.onValueChanged.RemoveAllListeners();
        paths_end.onValueChanged.RemoveAllListeners();
        instants_end.onValueChanged.RemoveAllListeners();
    }
}
