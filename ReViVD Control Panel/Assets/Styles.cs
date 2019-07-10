using UnityEngine.UI;
using UnityEngine;

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

    void Switch() {
        inputs_bounds.SetActive(!useMinMax.isOn);
        RectTransform rt = GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, rt.sizeDelta.y + (useMinMax.isOn ? -1 : 1) * 30);
        VerticalLayoutGroup lg = ScrollbarContent.Instance.GetComponent<VerticalLayoutGroup>();
        lg.spacing = lg.spacing + 1; //Forcing update of the layout group
        lg.spacing = lg.spacing - 1;
    }

    void OnEnable() {
        useMinMax.onValueChanged.AddListener(delegate { Switch(); });
    }

    private void OnDisable() {
        useMinMax.onValueChanged.RemoveAllListeners();
    }
}
