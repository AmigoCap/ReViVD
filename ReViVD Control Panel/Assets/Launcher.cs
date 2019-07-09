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

    public struct AssetBundle {
        string name;
        string filename;
        Vector3 position;
        Vector3 rotation;
        Vector3 scale;
    }

    public enum DataType {
        int32,
        float32,
        float64
    }

    public enum PathAttributeRole {
        n_atoms,
        id,
        other
    }

    public struct PathAttribute {
        string name;
        DataType type;
        PathAttributeRole role;
        bool randomColor;
        Color32 colorStart;
        Color32 colorEnd;
        float valueColorStart;
        float valueColorEnd;
    }

    public enum AtomAttributeRole {
        x,
        y,
        z,
        t,
        lat,
        lon,
        other
    }

    public struct AtomAttribute {
        string name;
        DataType type;
        AtomAttributeRole role;
        Color32 colorStart;
        Color32 colorEnd;
        float valueColorStart;
        float valueColorEnd;
    }

    public struct LoadingData {
        string filename;
        bool endianness;
        Vector3 sizeCoeff;
        Vector3 districtSize;
        Vector3 minPoint;
        Vector3 maxPoint;
        Vector2 gpsOrigin;

        int file_n_paths;
        bool randomPaths;
        int chosen_n_paths;
        int chosen_paths_start;
        int chosen_paths_end;
        int chosen_paths_step;

        bool constant_n_instants;
        int file_n_instants;
        int chosen_instants_start;
        int chosen_instants_end;
        int chosen_instants_step;

        float spheresRadius;
        float spheresAnimSpeed;
        float spheresGlobalTime;

        AssetBundle[] assetBundles;
        PathAttribute[] pathAttributes;
        AtomAttribute[] atomAttributes;
    };

    public void LoadJSon() {

    }

    void Awake() {
        if (_instance != null) {
            Debug.LogWarning("Multiple instances of logger singleton");
        }
        _instance = this;
    }
}
