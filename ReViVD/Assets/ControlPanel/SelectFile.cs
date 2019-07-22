using UnityEngine.UI;
using UnityEngine;
using System.IO;

namespace Revivd {
    [DisallowMultipleComponent]
    public class SelectFile : MonoBehaviour {
        public InputField field;

#pragma warning disable 0649
        [SerializeField] GameObject indicator_found;
        [SerializeField] GameObject indicator_notFound;
#pragma warning restore 0649

        void CheckForFile() {
            bool jsonExists = File.Exists(field.text) && new FileInfo(field.text).Extension == ".json";
            if (!indicator_found.activeSelf && !indicator_notFound.activeSelf) {
                RectTransform rt = GetComponent<RectTransform>();
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, rt.sizeDelta.y + 30);
                VerticalLayoutGroup lg = ScrollbarContent.Instance.GetComponent<VerticalLayoutGroup>();
                lg.spacing = lg.spacing + 1; //Forcing update of the layout group
                lg.spacing = lg.spacing - 1;
            }
            indicator_found.SetActive(jsonExists);
            indicator_notFound.SetActive(!jsonExists);

            if (jsonExists) {
                ControlPanel.Instance.workingDirectory = new FileInfo(field.text).Directory.FullName;
                ControlPanel.Instance.LoadJson();
            }
            else {
                ControlPanel.Instance.UnloadJson();
            }
        }

        private void OnEnable() {
            field.onValueChanged.AddListener(delegate { CheckForFile(); });
        }

        private void OnDisable() {
            field.onValueChanged.RemoveAllListeners();
        }
    }
}