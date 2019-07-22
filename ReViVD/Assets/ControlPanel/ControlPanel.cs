using UnityEngine.UI;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;

namespace Revivd {
    public class ControlPanel : MonoBehaviour {
        public GameObject errorWindow;

        private static ControlPanel _instance;
        public static ControlPanel Instance { get { return _instance; } }

        public SelectFile selectFile;
        public AxisConf axisConf;
        public Sampling sampling;
        public Spheres spheres;
        public Styles style;
        public Advanced advanced;

#pragma warning disable 0649
        [SerializeField] Button load;
        [SerializeField] Button export;
        [SerializeField] Button export_results;
#pragma warning restore 0649

        public class JsonData {
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
                public float positionOffset = 0;
                public float sizeCoeff = 1;
                public Color colorStart = Color.Blue;
                public Color colorEnd = Color.Red;
                public bool valueColorUseMinMax = true;
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
            public bool severalFiles_splitInstants = false;
            public int splitInstants_instantsPerFile = 50;
            //public bool severalFiles_splitPaths = false;
            //public int splitPaths_pathsPerFile = 1000;
            public string severalFiles_firstFileSuffix = "0001";

            public Endianness endianness = Endianness.little;
            public Vector3D districtSize = new Vector3D { x = 20, y = 20, z = 20 };
            public Vector3D lowerTruncature = new Vector3D { x = -1000, y = -1000, z = -1000 };
            public Vector3D upperTruncature = new Vector3D { x = 1000, y = 1000, z = 1000 };

            public int dataset_n_paths = int.MaxValue;
            public bool allPaths = false;
            public bool randomPaths = false;
            public int chosen_n_paths = 500;
            public int chosen_paths_start = 0;
            public int chosen_paths_end = 500;
            public int chosen_paths_step = 1;

            public int dataset_n_instants = int.MaxValue;
            public bool allInstants = false;
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

        public static Vector3 LDVector3_to_Vector3(ControlPanel.JsonData.Vector3D vector3D) {
            return new Vector3(vector3D.x, vector3D.y, vector3D.z);
        }
        public static Vector2 LDVector2_to_Vector2(ControlPanel.JsonData.Vector2D vector2D) {
            return new Vector2(vector2D.x, vector2D.y);
        }

        public static Color LDColor_to_Color(ControlPanel.JsonData.Color color) {
            switch (color) {
                case ControlPanel.JsonData.Color.Blue:
                    return Color.blue;
                case ControlPanel.JsonData.Color.Green:
                    return Color.green;
                default:
                    return Color.red;
            }
        }

        private bool _dataLoaded = false;
        public bool DataLoaded {
            get => _dataLoaded;
        }
        public JsonData data;
        System.IO.FileInfo dataInfo;

        public string workingDirectory;
        public JsonData LoadJson() {
            try {
                dataInfo = new FileInfo(selectFile.field.text);
                StreamReader r = new StreamReader(selectFile.field.text);
                string json = r.ReadToEnd();
                data = JsonConvert.DeserializeObject<JsonData>(json);
                if (!CleanupData(data)) {
                    throw new System.Exception("Data cleanup failed");
                }
            }
            catch (System.Exception e) {
                LogError("Error while loading .json, make sure it is valid\n\n" + e.Message);
                _dataLoaded = false;
                sampling.gameObject.SetActive(false);
                axisConf.gameObject.SetActive(false);
                spheres.gameObject.SetActive(false);
                style.gameObject.SetActive(false);
                advanced.gameObject.SetActive(false);
                load.interactable = false;
                export.interactable = false;
                export_results.interactable = false;
                return null;
            }

            _dataLoaded = false;

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
                JsonData.AtomAttribute attr = data.atomAttributes[i];
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

            sampling.gameObject.SetActive(true);
            axisConf.gameObject.SetActive(true);
            spheres.gameObject.SetActive(true);
            style.gameObject.SetActive(true);
            advanced.gameObject.SetActive(true);
            load.interactable = true;
            export.interactable = true;
            export_results.interactable = true;

            spheres.animate.interactable = false;
            spheres.drop.interactable = false;

            _dataLoaded = true;
            return data;
        }

        bool CleanupData(JsonData data) { //Fixes potential errors in the .json (ensures end > start, n_ values positive, etc.)

            if (data.severalFiles_splitInstants) {
                string filename = Tools.GetFullPath(data.filename + data.severalFiles_firstFileSuffix);
                if (!File.Exists(filename)) {
                    Debug.LogError("First data file not found at " + filename);
                    return false;
                }

                if (data.pathAttributeUsedAs_n_atoms != "")
                    Debug.LogWarning("Uncommon non-empty n_atom path attribute for a split-file dataset, is this intentional?");
            }
            else {
                string filename = Tools.GetFullPath(data.filename);
                if (!File.Exists(filename)) {
                    Debug.LogError("Data file not found at " + filename);
                    return false;
                }
            }


            void CheckValue<T>(ref T value, bool condition, T defaultvalue, string log) {
                if (condition) {
                    Debug.LogWarning(log + "Replacing with " + defaultvalue.ToString());
                    value = defaultvalue;
                }
            }

            void CheckValues_swap<T>(ref T lowValue, ref T highValue, string log) where T : System.IComparable {
                if (lowValue.CompareTo(highValue) > 0) {
                    T temp = lowValue;
                    lowValue = highValue;
                    highValue = temp;
                    Debug.LogWarning("Swapping values");
                }
            }

            CheckValue(ref data.districtSize.x, data.districtSize.x <= 0, 20, "Negative X value in District Size");
            CheckValue(ref data.districtSize.y, data.districtSize.y <= 0, 20, "Negative Y value in District Size");
            CheckValue(ref data.districtSize.z, data.districtSize.z <= 0, 20, "Negative Z value in District Size");

            if (data.lowerTruncature.x > data.upperTruncature.x ||
                data.lowerTruncature.y > data.upperTruncature.y ||
                data.lowerTruncature.z > data.upperTruncature.z) {
                Debug.LogError("lowerTruncature is not strictly inferior to upperTruncature, resetting to default values");
                data.lowerTruncature = new ControlPanel.JsonData.Vector3D { x = -1000, y = -1000, z = -1000 };
                data.upperTruncature = new ControlPanel.JsonData.Vector3D { x = 1000, y = 1000, z = 1000 };
            }

            if (data.dataset_n_paths <= 0) {
                Debug.LogError("Negative number of paths");
            }
            else if (!data.allPaths) {
                CheckValue(ref data.chosen_paths_start, data.chosen_paths_start < 0, 0, "Negative value for chosen_paths_start");
                CheckValue(ref data.chosen_paths_start, data.chosen_paths_start > data.dataset_n_paths, 0, "Chosen_paths_start value bigger than number of paths");

                CheckValue(ref data.chosen_paths_end, data.chosen_paths_end < 0, 500, "Negative value for chosen_paths_end");
                CheckValue(ref data.chosen_paths_end, data.chosen_paths_end > data.dataset_n_paths, 500, "Chosen paths end bigger than number of paths");

                CheckValues_swap(ref data.chosen_paths_start, ref data.chosen_paths_end, "Chosen paths start bigger than end");

                CheckValue(ref data.chosen_paths_step, data.chosen_paths_step < 1, 1, "Incorrect value for chosen_paths_step");

                if (data.randomPaths) {
                    CheckValue(ref data.chosen_n_paths, data.chosen_n_paths <= 0, 500, "Negative value for chosen_n_paths");
                    CheckValue(ref data.chosen_n_paths, data.chosen_n_paths > data.dataset_n_paths, 500, "Chosen_n_paths value bigger than number of paths");
                    if (data.chosen_n_paths > data.chosen_paths_end - data.chosen_paths_start) {
                        Debug.LogError("Asking for more random paths than the range allows");
                        Debug.LogWarning("Falling back to non-random paths in specified range");
                        data.randomPaths = false;
                        data.chosen_n_paths = data.chosen_paths_end - data.chosen_paths_start;
                        data.chosen_paths_step = 1;
                    }
                }
            }

            //constant_n_instants

            if (data.dataset_n_instants <= 0) {
                Debug.LogError("Negative number of instants");
            }
            else if (!data.allInstants) {
                CheckValue(ref data.chosen_instants_start, data.chosen_instants_start < 0, 0, "Negative value for chosen_instants_start");
                CheckValue(ref data.chosen_instants_start, data.chosen_instants_start > data.dataset_n_instants, 0, "Chosen_instants_start value bigger than number of instants");

                CheckValue(ref data.chosen_instants_end, data.chosen_instants_end < 0, data.dataset_n_instants, "Negative value for chosen_instants_end");
                CheckValue(ref data.chosen_instants_end, data.chosen_instants_end > data.dataset_n_instants, data.dataset_n_instants, "Chosen instants end bigger than number of instants");

                if (data.chosen_instants_start > data.chosen_instants_end) {
                    int temp = data.chosen_instants_end;
                    data.chosen_instants_end = data.chosen_instants_start;
                    data.chosen_instants_start = temp;
                }

                CheckValue(ref data.chosen_instants_step, data.chosen_instants_step < 1, 1, "Incorrect value for chosen_instants_step");
            }

            if (data.useGPSCoords) {
                CheckValue(ref data.GPSOrigin.x, Mathf.Abs(data.GPSOrigin.x) > 90, 0, "latitude in decimal degree out of range +-90°");
                CheckValue(ref data.GPSOrigin.y, Mathf.Abs(data.GPSOrigin.y) > 180, 0, "longitude in decimal degree out of range +-180°");
            }

            CheckValue(ref data.spheresRadius, data.spheresRadius < 0, 2, "Negative Value for spheresRadius");
            //spheresAnimSpeed --> can be negative
            // spheresGlobalTime --> can either be negative or positive
            // spheresDisplay
            //assetBundles --> we already check if file exists while opening it

            //pathAttributes
            bool CheckIfPathAttributeExists(string attribute, ControlPanel.JsonData.PathAttribute[] pathAttributes) {
                for (int i = 0; i < pathAttributes.Length; i++) {
                    if (pathAttributes[i].name == attribute)
                        return true;
                }
                return false;
            }

            if (data.pathAttributeUsedAs_id != "" && !CheckIfPathAttributeExists(data.pathAttributeUsedAs_id, data.pathAttributes)) {
                Debug.LogError("The path attribute to use as id doesn't exist");
            }
            if (data.pathAttributeUsedAs_n_atoms != "" && !CheckIfPathAttributeExists(data.pathAttributeUsedAs_n_atoms, data.pathAttributes)) {
                Debug.LogError("The path attribute to use as n_atoms doesn't exist");
            }

            if (data.pathAttributes.Length > 0
                + (data.pathAttributeUsedAs_id == "" ? 0 : 1)
                + (data.pathAttributeUsedAs_n_atoms == "" ? 0 : 1)) {
                Debug.LogWarning("Uncommon: some path attributes are unused, is this intentional?");
            }

            //atomAttributes
            bool CheckIfAtomAttributeExists(string attribute, ControlPanel.JsonData.AtomAttribute[] atomAttributes) {
                for (int i = 0; i < atomAttributes.Length; i++) {
                    if (atomAttributes[i].name == attribute)
                        return true;
                }
                return false;
            }

            if (data.atomAttributeUsedAs_x == "" && data.atomAttributeUsedAs_y == "" && data.atomAttributeUsedAs_z == "") {
                Debug.Log("No attributes used for any of the 3 dimensions");
                return false;
            }

            if (data.atomAttributeUsedAs_x != "" && !CheckIfAtomAttributeExists(data.atomAttributeUsedAs_x, data.atomAttributes)) {
                Debug.LogError("The atom attribute to use as x doesn't exist");
            }
            if (data.atomAttributeUsedAs_y != "" && !CheckIfAtomAttributeExists(data.atomAttributeUsedAs_y, data.atomAttributes)) {
                Debug.LogError("The atom attribute to use as y doesn't exist");
            }
            if (data.atomAttributeUsedAs_z != "" && !CheckIfAtomAttributeExists(data.atomAttributeUsedAs_z, data.atomAttributes)) {
                Debug.LogError("The atom attribute to use as z doesn't exist");
            }
            if (data.atomAttributeUsedAs_t != "" && !CheckIfAtomAttributeExists(data.atomAttributeUsedAs_t, data.atomAttributes)) {
                Debug.LogError("The atom attribute to use as t doesn't exist");
            }
            if (data.atomAttributeUsedAs_color != "" && !CheckIfAtomAttributeExists(data.atomAttributeUsedAs_color, data.atomAttributes)) {
                Debug.LogError("The atom attribute to use as color doesn't exist");
            }

            for (int i = 0; i < data.atomAttributes.Length; i++) {
                CheckValues_swap(ref data.atomAttributes[i].valueColorStart, ref data.atomAttributes[i].valueColorStart, "valueColorStart is bigger than valuecolorEnd for atom attribute " + data.atomAttributes[i].name);
            }

            return true;
        }

        void UpdateDataFromUI() { //Polls the UI for changes to the data class
            if (!DataLoaded) {
                LogError("Unexpected call to UpdateData() with non-loaded data");
                return;
            }

            data.randomPaths = sampling.randomPaths.isOn;
            data.allPaths = sampling.allPaths.isOn;
            data.allInstants = sampling.allInstants.isOn;
            data.chosen_n_paths = Tools.ParseField_i(sampling.n_paths, data.dataset_n_paths);
            data.chosen_paths_start = Tools.ParseField_i(sampling.paths_start, 0);
            data.chosen_paths_end = Tools.ParseField_i(sampling.paths_end, data.dataset_n_paths);
            data.chosen_paths_step = Tools.ParseField_i(sampling.paths_step, 1);
            data.chosen_instants_start = Tools.ParseField_i(sampling.instants_start, 0);
            data.chosen_instants_end = Tools.ParseField_i(sampling.instants_end, data.dataset_n_instants);
            data.chosen_instants_step = Tools.ParseField_i(sampling.instants_step, 1);

            data.useGPSCoords = axisConf.gps.isOn;
            if (axisConf.xAxis.value != 0) {
                JsonData.AtomAttribute attr = data.atomAttributes[axisConf.xAxis.value - 1];
                attr.sizeCoeff = Tools.ParseField_f(axisConf.xScale, 1f);
                data.atomAttributeUsedAs_x = attr.name;
            }
            if (axisConf.yAxis.value != 0) {
                JsonData.AtomAttribute attr = data.atomAttributes[axisConf.yAxis.value - 1];
                attr.sizeCoeff = Tools.ParseField_f(axisConf.yScale, 1f);
                data.atomAttributeUsedAs_y = attr.name;
            }
            if (axisConf.zAxis.value != 0) {
                JsonData.AtomAttribute attr = data.atomAttributes[axisConf.zAxis.value - 1];
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
                JsonData.AtomAttribute attr = data.atomAttributes[style.attribute.value - 1];
                attr.colorStart = (JsonData.Color)style.startColor.value;
                attr.colorEnd = (JsonData.Color)style.endColor.value;
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

        public void ExportJson() {
            UpdateDataFromUI();

            try {
                dataInfo.Directory.Create();
                StreamWriter w = new StreamWriter(System.IO.Path.Combine(workingDirectory, "export.json"));
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

        public void Launch() {
            if (!DataLoaded) {
                LogError("Attempted to launch main program without data being loaded properly");
                return;
            }

            UpdateDataFromUI();

            Visualization viz = Visualization.Instance;

            viz.Load();
        }

        public void LogError(string message) {
            GameObject error = Instantiate(errorWindow);
            error.transform.SetParent(this.transform.parent, false);
            error.GetComponent<ErrorWindow>().message.text = message;
        }

        private void Start() {
            selectFile.field.text = "..\\ReViVD\\External Data\\Bogey\\bogey.json";
        }

        private void OnEnable() {
            export.onClick.AddListener(ExportJson);
            load.onClick.AddListener(Launch);
            export_results.onClick.AddListener(Visualization.ExportResults);
        }

        private void OnDisable() {
            export.onClick.RemoveAllListeners();
            load.onClick.RemoveAllListeners();
            export_results.onClick.RemoveAllListeners();
        }

        void Awake() {
            if (_instance != null) {
                UnityEngine.Debug.LogWarning("Multiple instances of launcher singleton");
            }
            _instance = this;
        }

    }
}