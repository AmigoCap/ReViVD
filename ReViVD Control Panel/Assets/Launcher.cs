using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Launcher : MonoBehaviour
{
    private static Launcher _instance;
    public static Launcher Instance { get { return _instance; } }

    [SerializeField] GameObject selectFile;
    [SerializeField] GameObject sampling;
    [SerializeField] GameObject spheres;
    [SerializeField] GameObject style;


    public void LoadJSon() {

    }

    void Awake() {
        if (_instance != null) {
            Debug.LogWarning("Multiple instances of logger singleton");
        }
        _instance = this;
    }
}
