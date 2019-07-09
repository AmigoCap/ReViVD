using UnityEngine.UI;
using UnityEngine;
using System.IO;

[DisallowMultipleComponent]
public class SelectFile : MonoBehaviour
{
    public InputField field;
    public GameObject indicator_found;
    public GameObject indicator_notFound;

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
