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

    public class LoadingData {
        public struct Vector3D {
            public float x;
            public float y;
            public float z;
        }

        public struct Vector2D {
            public float x;
            public float y;
        }

        public class AssetBundle {
            public string name;
            public string filename;
            public Vector3D position;
            public Vector3D rotation;
            public Vector3D scale;
        }

        public enum DataType {
            int32,
            int64,
            float32,
            float64
        }

        public enum Color { //Warning : Keep this equal and in the same order to the color dropdowns in the launcher (enum to int conversions)
            Red,
            Green,
            Blue
        }

        public class PathAttribute {
            public string name;
            public DataType type = DataType.int32;
        }

        public string pathAttributeUsedAs_id = "id";
        public string pathAttributeUsedAs_n_atoms = "n_atoms";

        public class AtomAttribute {
            public string name;
            public DataType type = DataType.int32;
            public float sizeCoeff = 1;
            public bool valueColorUseMinMax = true;
            public Color colorStart = Color.Blue;
            public Color colorEnd = Color.Red;
            public float valueColorStart = 0;
            public float valueColorEnd = 1;
        }

        public string atomAttributeUsedAs_x = "x";
        public string atomAttributeUsedAs_y = "y";
        public string atomAttributeUsedAs_z = "z";
        public string atomAttributeUsedAs_t = "t";

        public enum Endianness {
            little,
            big
        }

        public string filename;
        public bool severalFiles = false;
        public int n_instants_per_file = 50;
        public Endianness endianness = Endianness.little;
        public Vector3D districtSize = new Vector3D { x = 20, y = 20, z = 20 };
        public Vector3D lowerTruncature = new Vector3D { x = -1000, y = -1000, z = -1000 };
        public Vector3D upperTruncature = new Vector3D { x = 1000, y = 1000, z = 1000 };

        public int file_n_paths;
        public bool randomPaths = false;
        public bool randomColorPaths = true;
        public int chosen_n_paths = 500;
        public int chosen_paths_start = 0;
        public int chosen_paths_end = 500;
        public int chosen_paths_step = 1;

        public bool constant_n_instants;
        public int file_n_instants;
        public int chosen_instants_start = 0;
        public int chosen_instants_end = 200;
        public int chosen_instants_step = 2;

        public bool useGPSCoords = false;
        public Vector2D GPSOrigin = new Vector2D { x = 0, y = 0 };

        public float spheresRadius = 2;
        public float spheresAnimSpeed = 1;
        public float spheresGlobalTime = 0;
        public bool spheresDisplay = false;

        public AssetBundle[] assetBundles = new AssetBundle[0];
        public PathAttribute[] pathAttributes = new PathAttribute[0];
        public AtomAttribute[] atomAttributes = new AtomAttribute[0];
    };

    private bool _dataLoaded = false;
    public bool DataLoaded {
        get => _dataLoaded;
    }
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

        for (int i = 0; i < data.atomAttributes.Length; i++) {
            LoadingData.AtomAttribute attr = data.atomAttributes[i];
            Dropdown.OptionData option = new Dropdown.OptionData(attr.name);
            foreach (Dropdown d in dropdowns)
                d.options.Add(option);
            if (attr.name == data.atomAttributeUsedAs_x) {
                axisConf.xAxis.value = i + 1;
            }
            else if (attr.name == data.atomAttributeUsedAs_y)
                axisConf.yAxis.value = i + 1;
            else if (attr.name == data.atomAttributeUsedAs_z)
                axisConf.zAxis.value = i + 1;
            else if (attr.name == data.atomAttributeUsedAs_t)
                axisConf.time.value = i + 1;
        }

        spheres.display.isOn = data.spheresDisplay;
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

        _dataLoaded = true;
    }

    public void SaveJson() {
        data.randomPaths = sampling.randomPaths.isOn;
        data.chosen_n_paths = Tools.ParseField_i(sampling.n_paths);
        data.chosen_paths_start = Tools.ParseField_i(sampling.paths_start);
        data.chosen_paths_end = Tools.ParseField_i(sampling.paths_end);
        data.chosen_paths_step = Tools.ParseField_i(sampling.paths_step);
        data.chosen_instants_start = Tools.ParseField_i(sampling.instants_start);
        data.chosen_instants_end = Tools.ParseField_i(sampling.instants_end);
        data.chosen_instants_step = Tools.ParseField_i(sampling.instants_step);

        if (axisConf.xAxis.value != 0) {
            data.atomAttributes[axisConf.xAxis.value - 1].sizeCoeff = Tools.ParseField_f(axisConf.xScale, 1f);
        }

        StreamWriter w = new StreamWriter("..\\ReViVD\\json\\export.json");
        JsonSerializerSettings settings = new JsonSerializerSettings();
        settings.FloatFormatHandling = FloatFormatHandling.String;
        settings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
        w.Write(JsonConvert.SerializeObject(data, Formatting.Indented, settings));
        w.Close();
    }

    private void Start() {
        selectFile.field.text = "..\\ReViVD\\json\\export.json";
    }

    private void OnDestroy() {
        SaveJson();
    }

    private void Update() {
    }

    void Awake() {
        if (_instance != null) {
            Debug.LogWarning("Multiple instances of launcher singleton");
        }
        _instance = this;
    }
}
