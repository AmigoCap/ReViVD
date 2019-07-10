using UnityEngine.UI;
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

    public struct LoadingData {
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
            public float sizeCoeff;
            public bool valueColorUseMinMax;
            public string colorStart;
            public string colorEnd;
            public float valueColorStart;
            public float valueColorEnd;
        }


        public string filename;
        public bool severalFiles;
        public int n_instants_per_file;
        public string endianness;
        public Vector3D districtSize;
        public Vector3D lowerTruncature;
        public Vector3D upperTruncature;
        public Vector2D gpsOrigin;

        public int file_n_paths;
        public bool randomPaths;
        public bool randomColorPaths;
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
        public bool spheresDisplay;

        public AssetBundle[] assetBundles;
        public PathAttribute[] pathAttributes;
        public AtomAttribute[] atomAttributes;
    };

    public LoadingData data;

    public void LoadJson() {
        StreamReader r = new StreamReader(selectFile.field.text);
        string json = r.ReadToEnd();
        data = JsonConvert.DeserializeObject<LoadingData>(json);

        sampling.randomPaths.isOn = data.randomPaths;
        sampling.n_paths.text = data.chosen_n_paths.ToString();
        sampling.paths_start.text = data.chosen_paths_start.ToString();
        sampling.paths_end.text = data.chosen_paths_end.ToString();
        sampling.paths_step.text = data.chosen_paths_step.ToString();
        sampling.instants_start.text = data.chosen_instants_start.ToString();
        sampling.instants_end.text = data.chosen_instants_end.ToString();
        sampling.instants_step.text = data.chosen_instants_step.ToString();

        Dropdown[] dropdowns = { axisConf.xAxis, axisConf.yAxis, axisConf.zAxis, style.attribute };
        Dropdown.OptionData emptyOption = new Dropdown.OptionData("<no attribute>");
        foreach (Dropdown d in dropdowns) {
            d.options.Clear();
            d.options.Add(emptyOption);
            d.value = 0;
            d.captionText.text = emptyOption.text;
        }

        foreach (LoadingData.AtomAttribute attr in data.atomAttributes) {
            Dropdown.OptionData option = new Dropdown.OptionData(attr.name);
            foreach (Dropdown d in dropdowns)
                d.options.Add(option);
        }

        //spheres.display.isOn = data.spheresDisplay;
        spheres.globalTime.text = data.spheresGlobalTime.ToString();
        spheres.animSpeed.text = data.spheresAnimSpeed.ToString();
        spheres.radius.text = data.spheresRadius.ToString();

        advanced.districtSize_x.text = data.districtSize.x.ToString();
        advanced.districtSize_y.text = data.districtSize.y.ToString();
        advanced.districtSize_z.text = data.districtSize.z.ToString();
        advanced.lowerTrunc_x.text = data.lowerTruncature.x.ToString();
        advanced.lowerTrunc_y.text = data.lowerTruncature.y.ToString();
        advanced.lowerTrunc_z.text = data.lowerTruncature.z.ToString();
        advanced.upperTrunc_x.text = data.upperTruncature.x.ToString();
        advanced.upperTrunc_y.text = data.upperTruncature.y.ToString();
        advanced.upperTrunc_z.text = data.upperTruncature.z.ToString();
    }

    private void Start() {
        selectFile.field.text = "..\\ReViVD\\json\\example.json";
    }

    void Awake() {
        if (_instance != null) {
            Debug.LogWarning("Multiple instances of launcher singleton");
        }
        _instance = this;
    }
}
