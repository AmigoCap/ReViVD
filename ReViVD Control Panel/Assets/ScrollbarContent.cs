using UnityEngine;

public class ScrollbarContent : MonoBehaviour
{
    private static ScrollbarContent _instance;
    public static ScrollbarContent Instance { get { return _instance; } }

    void Awake() {
        if (_instance != null) {
            Debug.LogWarning("Multiple instances of scrollbarcontent singleton");
        }
        _instance = this;
    }
}
