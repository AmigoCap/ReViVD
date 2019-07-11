using UnityEngine.UI;
using UnityEngine;
using System.IO;

[DisallowMultipleComponent]
public class SelectFile : MonoBehaviour
{
    public InputField field;

#pragma warning disable 0649
    [SerializeField] GameObject indicator_found;
    [SerializeField] GameObject indicator_notFound;
#pragma warning restore 0649

    void CheckForFile() {
        bool exists = File.Exists(field.text);
        if (!indicator_found.activeSelf && !indicator_notFound.activeSelf) {
            RectTransform rt = GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(rt.sizeDelta.x, rt.sizeDelta.y + 30);
            VerticalLayoutGroup lg = ScrollbarContent.Instance.GetComponent<VerticalLayoutGroup>();
            lg.spacing = lg.spacing + 1; //Forcing update of the layout group
            lg.spacing = lg.spacing - 1;
        }
        indicator_found.SetActive(exists);
        indicator_notFound.SetActive(!exists);

        if (exists) {
            Launcher.Instance.LoadJson();
        }
    }

    private void OnEnable() {
        field.onValueChanged.AddListener(delegate { CheckForFile(); });
    }

    private void OnDisable() {
        field.onValueChanged.RemoveAllListeners();
    }
}
