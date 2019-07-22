using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using Newtonsoft.Json;
using System.Globalization;

namespace Revivd {
    public class Visualization : MonoBehaviour {
        public List<Path> paths;

        public int GetPathIndex(string name) {
            int c = paths.Count;
            for (int i = 0; i < c; i++) {
                if (paths[i].name == name)
                    return i;
            }
            return -1;
        }

        static Visualization _instance;
        public static Visualization Instance { get { return _instance; } }

        public Vector3 districtSize;

        public Material material;

        //DEBUG
        public bool debugMode = false;
        public readonly HashSet<int[]>[] districtsToHighlight = { new HashSet<int[]>(new CoordsEqualityComparer()), new HashSet<int[]>(new CoordsEqualityComparer()), new HashSet<int[]>(new CoordsEqualityComparer()) };
        public bool clearDistrictsToHighlight = false;

        void OnDrawGizmos() {
            if (debugMode) {
                Gizmos.color = Color.red;
                foreach (int[] d in districtsToHighlight[0]) {
                    Gizmos.DrawWireCube(transform.TransformPoint(districtSize / 2 + new Vector3(d[0] * districtSize.x, d[1] * districtSize.y, d[2] * districtSize.z)), districtSize);
                }

                Gizmos.color = Color.green;
                foreach (int[] d in districtsToHighlight[1]) {
                    Gizmos.DrawWireCube(transform.TransformPoint(districtSize / 2 + new Vector3(d[0] * districtSize.x, d[1] * districtSize.y, d[2] * districtSize.z)), districtSize);
                }

                Gizmos.color = Color.blue;
                foreach (int[] d in districtsToHighlight[2]) {
                    Gizmos.DrawWireCube(transform.TransformPoint(districtSize / 2 + new Vector3(d[0] * districtSize.x, d[1] * districtSize.y, d[2] * districtSize.z)), districtSize);
                }
            }
        }

        bool _loaded = false;
        public bool Loaded { get => _loaded; }

        public void Load() {
            if (Loaded) {
                foreach (Transform child in this.transform)
                    Destroy(child.gameObject);
                paths.Clear();
                districts.Clear();
                clearDistrictsToHighlight = true;
                displayTimeSpheres = false;
                _loaded = false;
            }

            if (!LoadFromFile()) {
                Debug.LogError("Failed loading from file");
                _loaded = false;
            }
            else {
                Debug.Log("Successfully loaded file");
                _loaded = true;
                CreateDistricts();
            }
        }

        public struct District { //Subdivision discrète de la visualisation dans l'espace pour optimisation
            public List<Atom> atoms; //Tous les atomes dont le ruban fini traverse le district
        }

        public Dictionary<int[], District> districts;

        //Renvoie les coordonnées du district auquel appartient un certain point (exprimé dans le repère de la visualisation); ce district peut être null (pas d'entrée dans le dictionnaire).
        public int[] FindDistrictCoords(Vector3 point) {
            return new int[] {
            Mathf.FloorToInt(point.x / districtSize.x),
            Mathf.FloorToInt(point.y / districtSize.y),
            Mathf.FloorToInt(point.z / districtSize.z)
            };
        }

        //Raccourci pour obtenir le district auquel appartient un point du repère de la visualisation
        public District FindDistrict(Vector3 point) {
            return districts[FindDistrictCoords(point)];
        }

        //Renvoie le centre d'un district, même si celui-ci est fictif
        public Vector3 getDistrictCenter(int[] coords) {
            return new Vector3(districtSize.x * coords[0], districtSize.y * coords[1], districtSize.z * coords[2]) + districtSize / 2;
        }

        int[] _lowerDistrict = new int[] { 0, 0, 0 };
        public int[] LowerDistrict { get => _lowerDistrict; }

        int[] _upperDistrict = new int[] { 0, 0, 0 };
        public int[] UpperDistrict { get => _upperDistrict; }

        public bool districtWithinBoundaries(int[] districtCoords) {
            for (int i = 0; i < 3; i++) {
                if (districtCoords[i] < LowerDistrict[i])
                    return false;
                if (districtCoords[i] > UpperDistrict[i])
                    return false;
            }
            return true;
        }

        void FindDistrictBoundaries() {
            if (districts.Count == 0) {
                _lowerDistrict = new int[] { 0, 0, 0 };
                _upperDistrict = new int[] { 0, 0, 0 };
                return;
            }

            var e = districts.GetEnumerator();
            e.MoveNext();
            _lowerDistrict = (int[])e.Current.Key.Clone();
            _upperDistrict = (int[])e.Current.Key.Clone();

            while (e.MoveNext()) {
                for (int i = 0; i < 3; i++) {
                    _lowerDistrict[i] = Math.Min(_lowerDistrict[i], e.Current.Key[i]);
                    _upperDistrict[i] = Math.Max(_upperDistrict[i], e.Current.Key[i]);
                }
            }
        }

        void CreateDistricts() { //Crée et remplit districts
            districts = new Dictionary<int[], District>(new CoordsEqualityComparer());

            foreach (Path p in paths) {
                //Rappel : les coordonnées issues du csv (donc celles de Atom.point) sont les coordonnées relatives au Path dans le repère du Path.
                Vector3 point = transform.InverseTransformPoint(p.transform.TransformPoint(p.atoms[0].point));

                for (int i = 0; i < p.atoms.Count - 1; i++) {
                    Vector3 nextPoint = transform.InverseTransformPoint(p.transform.TransformPoint(p.atoms[i + 1].point));
                    Vector3 delta = (nextPoint - point).normalized;


                    //Algorithme d'Amanatides en 3D : on détermine tous les districts entre ces deux districts
                    if (!p.specialRadii.TryGetValue(i, out float radius))
                        radius = p.baseRadius;
                    HashSet<int[]> districts_segment = Tools.Amanatides(point, nextPoint, radius);
                    foreach (int[] c in districts_segment) {
                        if (!districts.TryGetValue(c, out District d)) {
                            d = new District() {
                                atoms = new List<Atom>()
                            };
                            districts.Add(c, d);
                        }
                        d.atoms.Add(p.atoms[i]);
                    }

                    point = nextPoint;
                }
            }

            FindDistrictBoundaries();
        }

        public bool displayTimeSpheres = false;

        public float globalTime = 0;

        public bool useGlobalTime = true;

        public bool doSphereDrop = false;

        float old_timeSphereRadius;
        public float timeSphereRadius = 1;

        public bool doTimeSphereAnimation = false;
        public float timeSphereAnimationSpeed = 10;

        bool old_traceTimeSpheres;
        public bool traceTimeSpheres = false;

        public void DropSpheres() {
            foreach (Path p in paths) {
                p.timeSphereDropped = false;
                p.timeSphereTime = timeSphereAnimationSpeed < 0 ? float.NegativeInfinity : float.PositiveInfinity;
            }
            foreach (Atom a in SelectorManager.Instance.selectedRibbons[(int)SelectorManager.Instance.CurrentColor]) {
                if (a.ShouldDisplay) {
                    Path p = (Path)a.path;
                    if (timeSphereAnimationSpeed < 0) {
                        p.timeSphereTime = Mathf.Max(p.timeSphereTime, a.time);
                    }
                    else {
                        p.timeSphereTime = Mathf.Min(p.timeSphereTime, a.time);
                    }
                    p.timeSphereDropped = true;
                }
            }
        }

        public enum ColorGroup { Red = 0, Green, Blue, Yellow, Cyan, Magenta };
        public static NumberFormatInfo nfi = new NumberFormatInfo();


        public static void ExportResults() {

            DateTime now = DateTime.Now;
            string dir = Logger.Instance.dirname;

            // Log all displayed path names
            string export = "export" + now.Day.ToString("00") + '-' + now.Month.ToString("00") + '-' + now.Year.ToString().Substring(2, 2) + "_" + now.Hour.ToString("00") + 'h' + now.Minute.ToString("00") + ".csv";

            StreamWriter displayExport = new StreamWriter(System.IO.Path.Combine(dir, export));
            string s = "Displayed,";
            foreach  (Path path in SelectorManager.Instance.pathsToKeep) { 
                s+= path.name + ',';
            }
            displayExport.WriteLine(s);

      
            void displayExportColor(ColorGroup c) {
                string exportcolor = "export" + c + now.Day.ToString("00") + '-' + now.Month.ToString("00") + '-' + now.Year.ToString().Substring(2, 2) + "_" + now.Hour.ToString("00") + 'h' + now.Minute.ToString("00") + ".csv";
                StreamWriter displayExportByColor = new StreamWriter(System.IO.Path.Combine(dir, exportcolor));
                HashSet<Path> currentColorPath = new HashSet<Path>();

                foreach (Atom a in SelectorManager.Instance.selectedRibbons[(int)c]) {
                    if (a.ShouldDisplay)
                        currentColorPath.Add(a.path);
                }

                string s_color = c.ToString() + ',';
                foreach (Path path in currentColorPath) {
                    s_color += path.name + ',';
                }
                displayExportByColor.WriteLine(s_color);
            }

            // Log all displayed path names by color
            displayExportColor(ColorGroup.Red);
            displayExportColor(ColorGroup.Green);
            displayExportColor(ColorGroup.Blue);
            displayExportColor(ColorGroup.Yellow);
            displayExportColor(ColorGroup.Cyan);
            displayExportColor(ColorGroup.Magenta);

        }

        void Awake() {
            if (_instance != null) {
                Debug.LogWarning("Multiple instances of visualization singleton");
            }
            _instance = this;

            if (material == null)
                material = Resources.Load<Material>("Materials/Ribbon");

            old_timeSphereRadius = timeSphereRadius;
            old_traceTimeSpheres = traceTimeSpheres;
        }

        void Update() {
            if (!Loaded)
                return;

            foreach (Path p in paths) {
                p.UpdatePath();
            }

            if (!traceTimeSpheres && old_traceTimeSpheres) {
                foreach (Path p in paths) {
                    foreach (Atom a in p.atoms) {
                        a.ShouldDisplayBecauseTime = true;
                    }
                }
                old_traceTimeSpheres = false;
            }

            if (displayTimeSpheres) {
                if (doSphereDrop) {
                    DropSpheres();
                    doSphereDrop = false;
                }

                if (timeSphereRadius != old_timeSphereRadius) {
                    foreach (Path p in paths) {
                        p.UpdateTimeSphereRadius();
                    }
                    old_timeSphereRadius = timeSphereRadius;
                }

                if (traceTimeSpheres && !old_traceTimeSpheres) {
                    foreach (Path p in paths) {
                        foreach (Atom a in p.atoms) {
                            a.ShouldDisplayBecauseTime = false;
                        }
                    }
                    old_traceTimeSpheres = true;
                }

                if (useGlobalTime && doTimeSphereAnimation) {
                    globalTime += timeSphereAnimationSpeed * Time.deltaTime;
                    ControlPanel.Instance.spheres.globalTime.text = globalTime.ToString();
                }

            }

            foreach (Path p in paths) {
                p.UpdateTimeSphere();
            }

            if (debugMode) {
                if (clearDistrictsToHighlight) {
                    clearDistrictsToHighlight = false;
                    foreach (HashSet<int[]> dth in districtsToHighlight) {
                        dth.Clear();
                    }
                }
            }
        }

        class BinaryReader_BigEndian : BinaryReader {
            public BinaryReader_BigEndian(System.IO.Stream stream) : base(stream) { }

            public override int ReadInt32() {
                var data = base.ReadBytes(4);
                Array.Reverse(data);
                return BitConverter.ToInt32(data, 0);
            }

            public override long ReadInt64() {
                var data = base.ReadBytes(8);
                Array.Reverse(data);
                return BitConverter.ToInt64(data, 0);
            }

            public override float ReadSingle() {
                var data = base.ReadBytes(4);
                Array.Reverse(data);
                return BitConverter.ToSingle(data, 0);
            }

            public override double ReadDouble() {
                var data = base.ReadBytes(8);
                Array.Reverse(data);
                return BitConverter.ToDouble(data, 0);
            }
        }

        bool LoadFromFile() {
            Tools.StartClock();

            ControlPanel.JsonData data = ControlPanel.Instance.data;

            int n_of_bytes_per_atom = 0;   //number of bytes that atom attributes take per atom
            int n_of_atomAttributes = data.atomAttributes.Length;
            int n_of_pathAttributes = data.pathAttributes.Length;

            for (int i = 0; i < n_of_atomAttributes; i++) {
                if (data.atomAttributes[i].type == ControlPanel.JsonData.DataType.float32 || data.atomAttributes[i].type == ControlPanel.JsonData.DataType.int32)
                    n_of_bytes_per_atom += 4;
                else
                    n_of_bytes_per_atom += 8;
            }

            float AllTimeMinimumOfColorAttribute = float.PositiveInfinity;
            float AllTimeMaximumOfColorAttribute = float.NegativeInfinity;

            float ReadAttribute_f(BinaryReader reader, ControlPanel.JsonData.DataType type) {
                switch (type) {
                    case ControlPanel.JsonData.DataType.float32:
                        return reader.ReadSingle();
                    case ControlPanel.JsonData.DataType.float64:
                        return (float)reader.ReadDouble();
                    case ControlPanel.JsonData.DataType.int32:
                        return (float)reader.ReadInt32();
                    case ControlPanel.JsonData.DataType.int64:
                        return (float)reader.ReadInt64();
                    default: //Never happens.
                        return 0f;
                }
            }

            int ReadAttribute_i(BinaryReader reader, ControlPanel.JsonData.DataType type) {
                switch (type) {
                    case ControlPanel.JsonData.DataType.float32:
                        return (int)reader.ReadSingle();
                    case ControlPanel.JsonData.DataType.float64:
                        return (int)reader.ReadDouble();
                    case ControlPanel.JsonData.DataType.int32:
                        return reader.ReadInt32();
                    case ControlPanel.JsonData.DataType.int64:
                        return (int)reader.ReadInt64();
                    default: //Never happens.
                        return 0;
                }
            }

            int N_RoleIndex = -1, ID_RoleIndex = -1;

            for (int i = 0; i < n_of_pathAttributes; i++) {
                var attr = data.pathAttributes[i];
                if (attr.name == data.pathAttributeUsedAs_n_atoms) {
                    N_RoleIndex = i;
                }
                if (attr.name == data.pathAttributeUsedAs_id) {
                    ID_RoleIndex = i;
                }
            }

            float[] atomAttributeValuesBuffer = new float[n_of_atomAttributes];
            int X_RoleIndex = -1, Y_RoleIndex = -1, Z_RoleIndex = -1, T_RoleIndex = -1, Color_RoleIndex = -1;

            for (int i = 0; i < n_of_atomAttributes; i++) {
                var attr = data.atomAttributes[i];
                if (attr.name == data.atomAttributeUsedAs_x) {
                    X_RoleIndex = i;
                }
                if (attr.name == data.atomAttributeUsedAs_y) {
                    Y_RoleIndex = i;
                }
                if (attr.name == data.atomAttributeUsedAs_z) {
                    Z_RoleIndex = i;
                }
                if (attr.name == data.atomAttributeUsedAs_t) {
                    T_RoleIndex = i;
                }
                if (attr.name == data.atomAttributeUsedAs_color) {
                    Color_RoleIndex = i;
                }
            }

            //Conversions done here instead of being done everytime for the same value for each atom
            Color32 startColor = Color.blue;
            Color32 endColor = Color.red;
            if (Color_RoleIndex != -1) {
                startColor = ControlPanel.LDColor_to_Color(data.atomAttributes[Color_RoleIndex].colorStart);
                endColor = ControlPanel.LDColor_to_Color(data.atomAttributes[Color_RoleIndex].colorEnd);
            }

            Vector3 lowerTruncature = ControlPanel.LDVector3_to_Vector3(data.lowerTruncature);
            Vector3 upperTruncature = ControlPanel.LDVector3_to_Vector3(data.upperTruncature);

            if (data.useGPSCoords)
                Tools.SetGPSOrigin(ControlPanel.LDVector2_to_Vector2(data.GPSOrigin));

            bool randomPaths = data.randomPaths;
            int chosen_n_paths = data.chosen_n_paths;
            int chosen_paths_start = data.chosen_paths_start;
            int chosen_paths_end = data.chosen_paths_end;
            int chosen_paths_step = data.chosen_paths_step;
            if (data.allPaths) {
                randomPaths = false;
                chosen_n_paths = data.dataset_n_paths;
                chosen_paths_start = 0;
                chosen_paths_end = data.dataset_n_paths;
                chosen_paths_step = 1;
            }

            int[] keptPaths;
            if (data.randomPaths) {
                keptPaths = new int[chosen_n_paths];
            }
            else {
                keptPaths = new int[(chosen_paths_end - chosen_paths_start) / chosen_paths_step];
            }

            if (data.randomPaths) {
                SortedSet<int> chosenRandomPaths = new SortedSet<int>(); // SortedSet because keptPaths should always be sorted
                System.Random rnd = new System.Random();
                for (int i = 0; i < keptPaths.Length; i++) {
                    while (!chosenRandomPaths.Add(rnd.Next(chosen_paths_start, chosen_paths_end))) { }
                }
                chosenRandomPaths.CopyTo(keptPaths);
            }
            else {
                for (int i = 0; i < keptPaths.Length; i++) {
                    keptPaths[i] = chosen_paths_start + i * chosen_paths_step;
                }
            }

            paths = new List<Path>(keptPaths.Length);
            Color32[] pathColors = new Color32[keptPaths.Length];
            for (int i = 0; i < keptPaths.Length; i++)
                pathColors[i] = UnityEngine.Random.ColorHSV();

            Tools.AddClockStop("Generated paths array");

            // Load Assets Bundles
            int n_of_assetBundles = data.assetBundles.Length;
            for (int i = 0; i < n_of_assetBundles; i++) {

                AssetBundle ab = AssetBundle.LoadFromFile(Tools.GetFullPath(data.assetBundles[i].filename));
                if (ab == null) {
                    Debug.LogWarning("Failed to load AssetBundle " + data.assetBundles[i].name);
                    continue;
                }

                GameObject[] prefabs = ab.LoadAllAssets<GameObject>();

                foreach (GameObject prefab in prefabs) {
                    if (data.assetBundles[i].overrideBundleTransform) {
                        prefab.transform.position = ControlPanel.LDVector3_to_Vector3(data.assetBundles[i].position);
                        prefab.transform.eulerAngles = ControlPanel.LDVector3_to_Vector3(data.assetBundles[i].rotation);
                        prefab.transform.localScale = ControlPanel.LDVector3_to_Vector3(data.assetBundles[i].scale);
                    }
                    GameObject go = Instantiate(prefab);
                    go.transform.SetParent(this.transform, true);
                }

                ab.Unload(false);
            }
            Tools.AddClockStop("Loaded assetBundles");

            string GetCompositeFilename(string filenameBase, string firstSuffix, int fileNumber) {
                if (fileNumber == 0)
                    return filenameBase + firstSuffix;
                int firstNumber = int.Parse(firstSuffix);
                fileNumber += firstNumber;
                string suffix = fileNumber.ToString();
                while (suffix.Length < firstSuffix.Length)
                    suffix = '0' + suffix;
                return filenameBase + suffix;
            }

            int chosen_instants_start = data.chosen_instants_start;
            int chosen_instants_end = data.chosen_instants_end;
            int chosen_instants_step = data.chosen_instants_step;
            if (data.allInstants) {
                chosen_instants_start = 0;
                chosen_instants_end = data.dataset_n_instants;
                chosen_instants_step = 1;
            }

            int fileStart = 0;
            int fileEnd = 1;

            if (data.severalFiles_splitInstants) {
                fileStart = chosen_instants_start / data.splitInstants_instantsPerFile;
                fileEnd = (chosen_instants_end - 1) / data.splitInstants_instantsPerFile + 1;
            }

            BinaryReader br = null;

            for (int i_file = fileStart; i_file < fileEnd; i_file++) {
                string currentFileName;

                if (data.severalFiles_splitInstants) {
                    currentFileName = Tools.GetFullPath(GetCompositeFilename(data.filename, data.severalFiles_firstFileSuffix, i_file));
                }
                else {
                    currentFileName = Tools.GetFullPath(data.filename);
                }

                if (br != null)
                    br.Close();
                try {
                    if (data.endianness == ControlPanel.JsonData.Endianness.big) {
                        br = new BinaryReader_BigEndian(File.Open(currentFileName, FileMode.Open));
                    }
                    else {
                        br = new BinaryReader(File.Open(currentFileName, FileMode.Open));
                    }
                }
                catch (Exception e) {
                    Debug.LogError("Couldn't load file " + currentFileName + "\n\n" + e.Message);
                    break;
                }
                Tools.AddClockStop("Loaded data file " + currentFileName);

                int currentPath = 0;
                for (int i_path = 0; i_path < keptPaths.Length; i_path++) {
                    if (br.BaseStream.Position >= br.BaseStream.Length) {
                        Debug.LogError("Reached EoF on loading paths after " + paths.Count + " paths");
                        break;
                    }

                    int readableInstants = 0;
                    int pathID = 0;

                    void ReadPathAttributes() {
                        //Default values
                        readableInstants = data.severalFiles_splitInstants ? data.splitInstants_instantsPerFile : data.dataset_n_instants;
                        pathID = keptPaths[i_path];

                        for (int j = 0; j < n_of_pathAttributes; j++) {
                            if (j == N_RoleIndex || j == ID_RoleIndex) {
                                int attributeValue = ReadAttribute_i(br, data.pathAttributes[j].type);

                                if (j == N_RoleIndex)
                                    readableInstants = attributeValue;
                                if (j == ID_RoleIndex)
                                    pathID = attributeValue;
                            }
                            else {
                                ReadAttribute_f(br, data.pathAttributes[j].type);
                            }
                        }
                    }

                    ReadPathAttributes();

                    while (currentPath < keptPaths[i_path]) {
                        br.BaseStream.Position += readableInstants * n_of_bytes_per_atom;
                        ReadPathAttributes();
                        currentPath++;
                    }

                    Path p;
                    if (i_file == fileStart) {
                        GameObject go;
                        go = new GameObject(pathID.ToString());
                        go.transform.parent = transform;
                        p = go.AddComponent<Path>();
                        p.atoms = new List<Atom>();
                        if (!data.severalFiles_splitInstants)
                            p.atoms.Capacity = Math.Min((chosen_instants_end - chosen_instants_start) / chosen_instants_step, (readableInstants - chosen_instants_start) / chosen_instants_step);
                        paths.Add(p);
                    }
                    else {
                        p = paths[i_path];
                    }

                    long nextPathPosition = br.BaseStream.Position + readableInstants * n_of_bytes_per_atom;

                    int localInstant = 0;
                    if (i_file == fileStart) {
                        localInstant = chosen_instants_start - i_file * data.splitInstants_instantsPerFile;
                        br.BaseStream.Position += localInstant * n_of_bytes_per_atom;
                    }

                    int instantsToRead = readableInstants;
                    if (i_file == fileEnd - 1) {
                        instantsToRead = Math.Min(instantsToRead, chosen_instants_end - i_file * data.splitInstants_instantsPerFile);
                    }

                    int atomIndex = p.atoms.Count;

                    while (localInstant < instantsToRead) {
                        Atom a = new Atom {
                            path = p,
                            indexInPath = atomIndex
                        };

                        for (int k = 0; k < n_of_atomAttributes; k++) {
                            atomAttributeValuesBuffer[k] = ReadAttribute_f(br, data.atomAttributes[k].type);
                        }

                        if (data.useGPSCoords) {
                            if (X_RoleIndex != -1 && Z_RoleIndex != -1) {
                                a.point = Tools.GPSToXYZ(new Vector2(atomAttributeValuesBuffer[X_RoleIndex], atomAttributeValuesBuffer[Z_RoleIndex]));
                            }
                        }
                        else {
                            if (X_RoleIndex != -1)
                                a.point.x = atomAttributeValuesBuffer[X_RoleIndex];
                            if (Z_RoleIndex != -1)
                                a.point.z = atomAttributeValuesBuffer[Z_RoleIndex];
                        }
                        if (Y_RoleIndex != -1)
                            a.point.y = atomAttributeValuesBuffer[Y_RoleIndex];

                        a.point.x += data.atomAttributes[X_RoleIndex].positionOffset;
                        a.point.y += data.atomAttributes[Y_RoleIndex].positionOffset;
                        a.point.z += data.atomAttributes[Z_RoleIndex].positionOffset;
                        a.point = Vector3.Max(a.point, lowerTruncature);
                        a.point = Vector3.Min(a.point, upperTruncature);
                        a.point.x *= data.atomAttributes[X_RoleIndex].sizeCoeff;
                        a.point.y *= data.atomAttributes[Y_RoleIndex].sizeCoeff;
                        a.point.z *= data.atomAttributes[Z_RoleIndex].sizeCoeff;

                        if (T_RoleIndex != -1)
                            a.time = atomAttributeValuesBuffer[T_RoleIndex];
                        else
                            a.time = (float)(chosen_instants_start + localInstant * chosen_instants_step);

                        if (Color_RoleIndex != -1) {
                            a.colorValue = atomAttributeValuesBuffer[Color_RoleIndex];
                            if (data.atomAttributes[Color_RoleIndex].valueColorUseMinMax) {
                                AllTimeMinimumOfColorAttribute = Mathf.Min(AllTimeMinimumOfColorAttribute, a.colorValue);
                                AllTimeMaximumOfColorAttribute = Mathf.Max(AllTimeMaximumOfColorAttribute, a.colorValue);
                            }
                            else {
                                ControlPanel.JsonData.AtomAttribute attr = data.atomAttributes[Color_RoleIndex];
                                a.BaseColor = Color32.Lerp(startColor, endColor, (a.colorValue - attr.valueColorStart) / (attr.valueColorEnd - attr.valueColorStart));
                            }
                        }
                        else {
                            a.BaseColor = pathColors[i_path];
                        }

                        p.atoms.Add(a);

                        atomIndex++;
                        localInstant += chosen_instants_step;
                        br.BaseStream.Position += (chosen_instants_step - 1) * n_of_bytes_per_atom; //Skip atoms if necessary
                    }

                    br.BaseStream.Position = nextPathPosition;

                    currentPath++;
                }
            }

            if (Color_RoleIndex != -1 && data.atomAttributes[Color_RoleIndex].valueColorUseMinMax) {
                for (int j = 0; j < paths.Count; j++) {
                    for (int i = 0; i < paths[j].atoms.Count; i++) {
                        Atom a = paths[j].atoms[i];
                        a.BaseColor = Color32.Lerp(startColor, endColor, (a.colorValue - AllTimeMinimumOfColorAttribute) / (AllTimeMaximumOfColorAttribute - AllTimeMinimumOfColorAttribute));
                    }
                }
            }

            Tools.EndClock("Loaded paths");

            return true;
        }

    }
}
