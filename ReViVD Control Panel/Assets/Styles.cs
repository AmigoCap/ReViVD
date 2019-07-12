using UnityEngine.UI;
using UnityEngine;
using System.Collections.Generic;

public class Styles : MonoBehaviour
{
    public Dropdown attribute;
    public Dropdown startColor;
    public Dropdown endColor;
    public Toggle useMinMax;
    public InputField startValue;
    public InputField endValue;

#pragma warning disable 0649
    [SerializeField] GameObject inputs_bounds;
#pragma warning restore 0649

    void expandBox() {
        inputs_bounds.SetActive(!useMinMax.isOn);
        RectTransform rt = GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, rt.sizeDelta.y + (useMinMax.isOn ? -1 : 1) * 30);
        VerticalLayoutGroup lg = ScrollbarContent.Instance.GetComponent<VerticalLayoutGroup>();
        lg.spacing = lg.spacing + 1; //Forcing update of the layout group
        lg.spacing = lg.spacing - 1;
    }

    int previousValue = 0;
    void autofillValues() {
        if (Launcher.Instance.DataLoaded) {
            if (previousValue != 0) { //Save changed data so that it is not lost on switching
                var prev_attr = Launcher.Instance.data.atomAttributes[previousValue - 1];
                prev_attr.colorStart = (Launcher.LoadingData.Color)startColor.value;
                prev_attr.colorEnd = (Launcher.LoadingData.Color)endColor.value;
                prev_attr.valueColorStart = Tools.ParseField_f(startValue, 0f);
                prev_attr.valueColorEnd = Tools.ParseField_f(endValue, 1f);
                prev_attr.valueColorUseMinMax = useMinMax.isOn;
            }
            if (attribute.value == 0) {
                startColor.value = 0;
                endColor.value = 2;
                startValue.text = "";
                endValue.text = "";
                useMinMax.isOn = true;
            }
            else {
                var attr = Launcher.Instance.data.atomAttributes[attribute.value - 1];
                startColor.value = (int)attr.colorStart;
                endColor.value = (int)attr.colorEnd;
                startValue.text = attr.valueColorStart.ToString();
                endValue.text = attr.valueColorEnd.ToString();
                useMinMax.isOn = attr.valueColorUseMinMax;
            }

            previousValue = attribute.value;
        }
    }

    void OnEnable() {
        useMinMax.onValueChanged.AddListener(delegate { expandBox(); });
        attribute.onValueChanged.AddListener(delegate { autofillValues(); });
    }

    private void OnDisable() {
        useMinMax.onValueChanged.RemoveAllListeners();
        attribute.onValueChanged.RemoveAllListeners();
    }
}
