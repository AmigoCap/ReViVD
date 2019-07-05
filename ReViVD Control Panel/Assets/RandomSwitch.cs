using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RandomSwitch : MonoBehaviour
{
    public Toggle toggle;
    public GameObject randomLine;
    public GameObject standardLine;
    public RectTransform rangeLineTransform;

    float xPosRandom;
    float xPosStandard;

    void Switch() {
        randomLine.SetActive(toggle.isOn);
        standardLine.SetActive(!toggle.isOn);
        rangeLineTransform.localPosition = new Vector3(toggle.isOn ? xPosRandom : xPosStandard, rangeLineTransform.localPosition.y, 0);
    }

    void Start()
    {
        xPosRandom = rangeLineTransform.localPosition.x;
        xPosStandard = xPosRandom - 185;
        toggle.onValueChanged.AddListener(delegate { Switch(); });
    }
}
