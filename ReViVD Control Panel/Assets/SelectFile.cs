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
        indicator_found.SetActive(exists);
        indicator_notFound.SetActive(!exists);
    }

    // Start is called before the first frame update
    void Start()
    {
        field.onValueChanged.AddListener(delegate { CheckForFile(); });
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
