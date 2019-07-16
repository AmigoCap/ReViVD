using UnityEngine.UI;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;
using System.Diagnostics;

public class Launcher : MonoBehaviour
{
    public GameObject errorWindow;

    private static Launcher _instance;
    public static Launcher Instance { get { return _instance; } }

#pragma warning disable 0649
    [SerializeField] SelectFile selectFile;
    [SerializeField] AxisConf axisConf;
    [SerializeField] Sampling sampling;
    [SerializeField] Spheres spheres;
    [SerializeField] Styles style;
    [SerializeField] Advanced advanced;
    [SerializeField] Button launch;
    [SerializeField] Button export;
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
            public string name = "";
            public string filename = "";
            public bool overrideBundleTransform = false;
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

        public string pathAttributeUsedAs_id = "";
        public string pathAttributeUsedAs_n_atoms = "";

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

        public string atomAttributeUsedAs_x = "";
        public string atomAttributeUsedAs_y = "";
        public string atomAttributeUsedAs_z = "";
        public string atomAttributeUsedAs_t = "";
        public string atomAttributeUsedAs_color = "";

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

        public int file_n_paths = int.MaxValue;
        public bool randomPaths = false;
        public bool allPaths = false;
        public bool allInstants = false;
        public bool randomColorPaths = true;
        public int chosen_n_paths = 500;
        public int chosen_paths_start = 0;
        public int chosen_paths_end = 500;
        public int chosen_paths_step = 1;

        public bool constant_n_instants;
        public int file_n_instants = int.MaxValue;
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
    System.IO.FileInfo dataInfo;

    public void LoadJson() {
        try {
            dataInfo = new FileInfo(selectFile.field.text);
            StreamReader r = new StreamReader(selectFile.field.text);
            string json = r.ReadToEnd();
            data = JsonConvert.DeserializeObject<LoadingData>(json);
        }
        catch (System.Exception e) {
            LogError("Error while loading .json, make sure it is valid\n\n" + e.Message);
            _dataLoaded = false;
            sampling.gameObject.SetActive(false);
            axisConf.gameObject.SetActive(false);
            spheres.gameObject.SetActive(false);
            style.gameObject.SetActive(false);
            advanced.gameObject.SetActive(false);
            launch.interactable = false;
            export.interactable = false;
            return;
        }

        sampling.gameObject.SetActive(true);
        axisConf.gameObject.SetActive(true);
        spheres.gameObject.SetActive(true);
        style.gameObject.SetActive(true);
        advanced.gameObject.SetActive(true);
        launch.interactable = true;
        export.interactable = true;

        sampling.randomPaths.isOn = data.randomPaths;
        sampling.allPaths.isOn = data.allPaths;
        sampling.allInstants.isOn = data.allInstants;
        sampling.n_paths.text = data.chosen_n_paths.ToString();
        sampling.paths_start.text = data.chosen_paths_start.ToString();
        sampling.paths_end.text = data.chosen_paths_end.ToString();
        sampling.paths_step.text = data.chosen_paths_step.ToString();
        sampling.instants_start.text = data.chosen_instants_start.ToString();
        sampling.instants_end.text = data.chosen_instants_end.ToString();
        sampling.instants_step.text = data.chosen_instants_step.ToString();

        axisConf.gps.isOn = data.useGPSCoords;
        Dropdown[] dropdowns = { axisConf.xAxis, axisConf.yAxis, axisConf.zAxis, axisConf.time, style.attribute };
        Dropdown.OptionData emptyOption = new Dropdown.OptionData("<no attribute>");
        foreach (Dropdown d in dropdowns) {
            d.options.Clear();
            d.options.Add(emptyOption);
            d.value = 0;
            d.captionText.text = emptyOption.text;
        }

        for (int i = 0; i < data.atomAttributes.Length; i++) {
            LoadingData.AtomAttribute attr = data.atomAttributes[i];
            if (attr.name == null || attr.name == "")
                attr.name = "unnamed";
            Dropdown.OptionData option = new Dropdown.OptionData(attr.name);
            foreach (Dropdown d in dropdowns)
                d.options.Add(option);
            if (attr.name == data.atomAttributeUsedAs_x) {
                axisConf.xAxis.value = i + 1;
                axisConf.prevValue_x = i + 1;
                axisConf.xScale.text = attr.sizeCoeff.ToString();
            }
            if (attr.name == data.atomAttributeUsedAs_y) {
                axisConf.yAxis.value = i + 1;
                axisConf.prevValue_y = i + 1;
                axisConf.yScale.text = attr.sizeCoeff.ToString();
            }
            if (attr.name == data.atomAttributeUsedAs_z) {
                axisConf.zAxis.value = i + 1;
                axisConf.prevValue_z = i + 1;
                axisConf.zScale.text = attr.sizeCoeff.ToString();
            }
            if (attr.name == data.atomAttributeUsedAs_t) {
                axisConf.time.value = i + 1;
            }
            if (attr.name == data.atomAttributeUsedAs_color) {
                style.attribute.value = i + 1;
                style.startColor.value = (int)attr.colorStart;
                style.endColor.value = (int)attr.colorEnd;
                style.useMinMax.isOn = attr.valueColorUseMinMax;
                style.startValue.text = attr.valueColorStart.ToString();
                style.endValue.text = attr.valueColorEnd.ToString();
            }
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

    void UpdateData() { //Polls the UI for changes to the data class
        if (!DataLoaded) {
            LogError("Unexpected call to UpdateData() with non-loaded data");
            return;
        }

        data.randomPaths = sampling.randomPaths.isOn;
        data.allPaths = sampling.allPaths.isOn;
        data.allInstants = sampling.allInstants.isOn;
        data.chosen_n_paths = Tools.ParseField_i(sampling.n_paths, data.file_n_paths);
        data.chosen_paths_start = Tools.ParseField_i(sampling.paths_start, 0);
        data.chosen_paths_end = Tools.ParseField_i(sampling.paths_end, data.file_n_paths);
        data.chosen_paths_step = Tools.ParseField_i(sampling.paths_step, 1);
        data.chosen_instants_start = Tools.ParseField_i(sampling.instants_start, 0);
        data.chosen_instants_end = Tools.ParseField_i(sampling.instants_end, data.file_n_instants);
        data.chosen_instants_step = Tools.ParseField_i(sampling.instants_step, 1);

        data.useGPSCoords = axisConf.gps.isOn;
        if (axisConf.xAxis.value != 0) {
            LoadingData.AtomAttribute attr = data.atomAttributes[axisConf.xAxis.value - 1];
            attr.sizeCoeff = Tools.ParseField_f(axisConf.xScale, 1f);
            data.atomAttributeUsedAs_x = attr.name;
        }
        if (axisConf.yAxis.value != 0) {
            LoadingData.AtomAttribute attr = data.atomAttributes[axisConf.yAxis.value - 1];
            attr.sizeCoeff = Tools.ParseField_f(axisConf.yScale, 1f);
            data.atomAttributeUsedAs_y = attr.name;
        }
        if (axisConf.zAxis.value != 0) {
            LoadingData.AtomAttribute attr = data.atomAttributes[axisConf.zAxis.value - 1];
            attr.sizeCoeff = Tools.ParseField_f(axisConf.zScale, 1f);
            data.atomAttributeUsedAs_z = attr.name;
        }
        if (axisConf.time.value != 0) {
            data.atomAttributeUsedAs_t = data.atomAttributes[axisConf.time.value - 1].name;
        }

        data.spheresDisplay = spheres.display.isOn;
        data.spheresGlobalTime = Tools.ParseField_f(spheres.globalTime, 0);
        data.spheresAnimSpeed = Tools.ParseField_f(spheres.animSpeed, 1);
        data.spheresRadius = Tools.ParseField_f(spheres.radius, 2);

        if (style.attribute.value != 0) {
            LoadingData.AtomAttribute attr = data.atomAttributes[style.attribute.value - 1];
            attr.colorStart = (LoadingData.Color)style.startColor.value;
            attr.colorEnd = (LoadingData.Color)style.endColor.value;
            attr.valueColorStart = Tools.ParseField_f(style.startValue, 0f);
            attr.valueColorEnd = Tools.ParseField_f(style.endValue, 1f);
            attr.valueColorUseMinMax = style.useMinMax.isOn;
            data.atomAttributeUsedAs_color = attr.name;
        }

        data.districtSize.x = Tools.ParseField_f(advanced.districtSize_x, 20);
        data.districtSize.y = Tools.ParseField_f(advanced.districtSize_y, 20);
        data.districtSize.z = Tools.ParseField_f(advanced.districtSize_z, 20);

        data.lowerTruncature.x = Tools.ParseField_f(advanced.lowerTrunc_x, -1000);
        data.lowerTruncature.y = Tools.ParseField_f(advanced.lowerTrunc_y, -1000);
        data.lowerTruncature.z = Tools.ParseField_f(advanced.lowerTrunc_z, -1000);

        data.upperTruncature.x = Tools.ParseField_f(advanced.upperTrunc_x, 1000);
        data.upperTruncature.y = Tools.ParseField_f(advanced.upperTrunc_y, 1000);
        data.upperTruncature.z = Tools.ParseField_f(advanced.upperTrunc_z, 1000);
    }

    public void SaveJson() {
        UpdateData();

        try {
            dataInfo.Directory.Create();
            StreamWriter w = new StreamWriter(dataInfo.DirectoryName + "\\export.json");
            JsonSerializerSettings settings = new JsonSerializerSettings();
            settings.FloatFormatHandling = FloatFormatHandling.String;
            settings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
            w.Write(JsonConvert.SerializeObject(data, Formatting.Indented, settings));
            w.Close();
        }
        catch (System.Exception e) {
            LogError("Error exporting .json: ensure export.json is not being used by another process\n\n" + e.Message);
        }
    }

    public string revivdPath;
    Process revivd;
    bool started = false;

    public void Launch() {
        if (!DataLoaded) {
            LogError("Attempted to launch main program without data being loaded properly");
            return;
        }

        revivd = new Process {
            StartInfo = new ProcessStartInfo() {
                FileName = revivdPath,
                RedirectStandardInput = true,
                UseShellExecute = false
            }
        };
        try {
            revivd.Start();
        }
        catch (System.Exception e) {
            LogError("Failed to launch main program\n\n" + e.Message);
            return;
        }

        started = true;

        string json;
        try {
            json = JsonConvert.SerializeObject(data, Formatting.None);
        }
        catch (System.Exception e) {
            LogError("Error serializing data for IPC\n\n" + e.Message);
            return;
        }

        try {
            revivd.StandardInput.WriteLine(json);
        }
        catch (System.Exception e) {
            LogError("Error during IPC\n\n" + e.Message);
        }

    }

    public enum Command {
        DisplaySpheres = 0,     //bool
        AnimSpheres,            //bool
        DropSpheres,            //void
        SetSpheresGlobalTime,   //float
        UseGlobalTime,          //void
        SetSpheresAnimSpeed,    //float
        SetSpheresRadius,       //float
        Stop                    //void
    }

    public void TransmitCommand(Command c) {
        if (!started)
            return;
        try {
            revivd.StandardInput.Write((int)c);
        }
        catch (System.Exception e) {
            LogError("Error durring live IPC, command " + (int)c + "\n\n" + e.Message);
        }
    }

    public void TransmitCommand<T>(Command c, T value) {
        if (!started)
            return;
        try {
            revivd.StandardInput.Write((int)c);
            revivd.StandardInput.WriteLine(value);
        }
        catch (System.Exception e) {
            LogError("Error durring live IPC, command " + (int)c + "\n\n" + e.Message);
        }
    }

    public void LogError(string message) {
        GameObject error = Instantiate(errorWindow);
        error.transform.SetParent(this.transform.parent, false);
        error.GetComponent<ErrorWindow>().message.text = message;
    }

    private void Start() {
        selectFile.field.text = "..\\..\\ReViVD\\json\\example.json";
    }

    private void OnEnable() {
        export.onClick.AddListener(SaveJson);
        launch.onClick.AddListener(Launch);
    }

    private void OnDisable() {
        export.onClick.RemoveAllListeners();
        launch.onClick.RemoveAllListeners();
    }

    void Awake() {
        if (_instance != null) {
            UnityEngine.Debug.LogWarning("Multiple instances of launcher singleton");
        }
        _instance = this;
    }

}
