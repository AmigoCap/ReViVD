using UnityEngine.UI;
using UnityEngine;

namespace Revivd {
    public class AxisConf : MonoBehaviour {
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

        void AutoFillScale(Dropdown axis, ref int prevValue, InputField scale) {
            if (ControlPanel.Instance.Loaded) {
                if (prevValue != 0) {
                    ControlPanel.Instance.data.atomAttributes[prevValue - 1].sizeCoeff = Tools.ParseField_f(scale, 1f);
                }
                if (axis.value == 0)
                    scale.text = "";
                else
                    scale.text = ControlPanel.Instance.data.atomAttributes[axis.value - 1].sizeCoeff.ToString();
                prevValue = axis.value;
            }
        }

        private void OnEnable() {
            xAxis.onValueChanged.AddListener(delegate { AutoFillScale(xAxis, ref prevValue_x, xScale); });
            yAxis.onValueChanged.AddListener(delegate { AutoFillScale(yAxis, ref prevValue_y, yScale); });
            zAxis.onValueChanged.AddListener(delegate { AutoFillScale(zAxis, ref prevValue_z, zScale); });
        }

        private void OnDisable() {
            xAxis.onValueChanged.RemoveAllListeners();
            yAxis.onValueChanged.RemoveAllListeners();
            zAxis.onValueChanged.RemoveAllListeners();
        }
    }
}