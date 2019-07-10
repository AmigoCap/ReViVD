using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;

public class Launcher : MonoBehaviour
{
    private static Launcher _instance;
    public static Launcher Instance { get { return _instance; } }

#pragma warning disable 0649
    [SerializeField] SelectFile selectFile;
    [SerializeField] AxisConf axisConf;
    [SerializeField] Sampling sampling;
    [SerializeField] Spheres spheres;
    [SerializeField] Styles style;
    [SerializeField] Advanced advanced;
#pragma warning restore 0649

    public struct Vector3D {
        public float x;
        public float y;
        public float z;
    }
    public struct Vector2D {
        public float x;
        public float y;
    }


    public struct AssetBundle {
        public string name;
        public string filename;
        public Vector3D position;
        public Vector3D rotation;
        public Vector3D scale;
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
        public string name;
        public DataType type;
        public PathAttributeRole role;
        public bool randomColor;
        public string colorStart;
        public string colorEnd;
        public float valueColorStart;
        public float valueColorEnd;
    }

    public enum AtomAttributeRole {
        x,
        y,
        z,
        t,
        latitude,
        longitude,
        other
    }

    public struct AtomAttribute {
        public string name;
        public DataType type;
        public AtomAttributeRole role;
        public string colorStart;
        public string colorEnd;
        public float valueColorStart;
        public float valueColorEnd;
    }

    public struct LoadingData {
        public string filename;
        public bool severalFiles;
        public int n_instants_per_file;
        public string endianness;
        public Vector3D sizeCoeff;
        public Vector3D districtSize;
        public Vector3D minPoint;
        public Vector3D maxPoint;
        public Vector2D gpsOrigin;

        public int file_n_paths;
        public bool randomPaths;
        public int chosen_n_paths;
        public int chosen_paths_start;
        public int chosen_paths_end;
        public int chosen_paths_step;

        public bool constant_n_instants;
        public int file_n_instants;
        public int chosen_instants_start;
        public int chosen_instants_end;
        public int chosen_instants_step;

        public float spheresRadius;
        public float spheresAnimSpeed;
        public float spheresGlobalTime;

        public AssetBundle[] assetBundles;
        public PathAttribute[] pathAttributes;
        public AtomAttribute[] atomAttributes;
    };

    void LoadJSon() {
        StreamReader r = new StreamReader(selectFile.field.text);
        string json = r.ReadToEnd();
        LoadingData loadingdata = JsonConvert.DeserializeObject<LoadingData>(json);

    }

    void PrePopulate() {

    }

    void Awake() {
        if (_instance != null) {
            Debug.LogWarning("Multiple instances of launcher singleton");
        }
        _instance = this;
    }
}
