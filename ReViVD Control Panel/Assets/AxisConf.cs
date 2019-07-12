using UnityEngine.UI;
using UnityEngine;

public class AxisConf : MonoBehaviour
{
    public Dropdown xAxis;
    public int prevValue_x = 0;
    public InputField xScale;

    public Dropdown yAxis;
    public int prevValue_y = 0;
    public InputField yScale;

    public Dropdown zAxis;
    public int prevValue_z = 0;
    public InputField zScale;

    public Dropdown time;

    public Toggle gps;

    void autofillScale(Dropdown axis, ref int prevValue, InputField scale) {
        if (Launcher.Instance.DataLoaded) {
            if (prevValue != 0) {
                Launcher.Instance.data.atomAttributes[prevValue - 1].sizeCoeff = Tools.ParseField_f(scale, 1f);
            }
            if (axis.value == 0)
                scale.text = "";
            else
                scale.text = Launcher.Instance.data.atomAttributes[axis.value - 1].sizeCoeff.ToString();
            prevValue = axis.value;
        }
    }

    private void OnEnable() {
        xAxis.onValueChanged.AddListener(delegate { autofillScale(xAxis, ref prevValue_x, xScale); });
        yAxis.onValueChanged.AddListener(delegate { autofillScale(yAxis, ref prevValue_y, yScale); });
        zAxis.onValueChanged.AddListener(delegate { autofillScale(zAxis, ref prevValue_z, zScale); });
    }

    private void OnDisable() {
        xAxis.onValueChanged.RemoveAllListeners();
        yAxis.onValueChanged.RemoveAllListeners();
        zAxis.onValueChanged.RemoveAllListeners();
    }
}
