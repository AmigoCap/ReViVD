using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using Newtonsoft.Json;

namespace Revivd {
    public class GlobalVisualization : TimeVisualization {
        public List<GlobalPath> paths;
        public override IReadOnlyList<Path> PathsAsBase { get { return paths; } }
        public override IReadOnlyList<TimePath> PathsAsTime { get { return paths; } }

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

        private Vector3 LDVector3_to_Vector3(IPCReceiver.LoadingData.Vector3D vector3D) {
            return new Vector3(vector3D.x, vector3D.y, vector3D.z);
        }
        private Vector2 LDVector2_to_Vector2(IPCReceiver.LoadingData.Vector2D vector2D) {
            return new Vector2(vector2D.x, vector2D.y);
        }

        private Color LDColor_to_Color(IPCReceiver.LoadingData.Color color) {
            switch (color) {
                case IPCReceiver.LoadingData.Color.Blue:
                    return Color.blue;
                case IPCReceiver.LoadingData.Color.Green:
                    return Color.green;
                default:
                    return Color.red;
            }
        }

        string GetFullPath(string filename) {
            return System.IO.Path.Combine(GetComponent<IPCReceiver>().workingDirectory, filename);
        }

        bool CleanupData(IPCReceiver.LoadingData data) { //Fixes potential errors in the .json (ensures end > start, n_ values positive, etc.)
        
            if (data.severalFiles_splitInstants) {
                string filename = GetFullPath(data.filename + data.severalFiles_firstFileSuffix);
                if (!File.Exists(filename)) {
                    Debug.LogError("First data file not found at " + filename);
                    return false;
                }

                if (data.pathAttributeUsedAs_n_atoms != "")
                    Debug.LogWarning("Uncommon non-empty n_atom path attribute for a split-file dataset, is this intentional?");
            }
            else {
                string filename = GetFullPath(data.filename);
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

            void CheckValues_swap<T>(ref T lowValue, ref T highValue, string log) where T : IComparable {
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
                data.lowerTruncature = new IPCReceiver.LoadingData.Vector3D { x = -1000, y = -1000, z = -1000 };
                data.upperTruncature = new IPCReceiver.LoadingData.Vector3D { x = 1000, y = 1000, z = 1000 };
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
            bool CheckIfPathAttributeExists(string attribute, IPCReceiver.LoadingData.PathAttribute[] pathAttributes) {
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
            bool CheckIfAtomAttributeExists(string attribute, IPCReceiver.LoadingData.AtomAttribute[] atomAttributes) {
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

            for (int i=0; i < data.atomAttributes.Length; i++) {
                CheckValue(ref data.atomAttributes[i].sizeCoeff, data.atomAttributes[i].sizeCoeff < 0, 1, "The size coeff of atom attribute " + data.atomAttributes[i].name + " is negative");

                CheckValues_swap(ref data.atomAttributes[i].valueColorStart, ref data.atomAttributes[i].valueColorStart, "valueColorStart is bigger than valuecolorEnd for atom attribute " + data.atomAttributes[i].name);
            }

            return true;
        }

        IPCReceiver.LoadingData data;
        public bool manualLoading = false;
        public string json = "";
        protected override bool LoadFromFile() {
            Tools.StartClock();
            
            if (!manualLoading) {
                data = GetComponent<IPCReceiver>().CatchData();
                Tools.AddClockStop("Received json data");
            }
            else {
                GetComponent<IPCReceiver>().workingDirectory = new FileInfo(json).Directory.FullName;
                StreamReader reader = new StreamReader(json);
                data = JsonConvert.DeserializeObject<IPCReceiver.LoadingData>(reader.ReadToEnd());
                Tools.AddClockStop("Loaded json data");
            }

            if (!CleanupData(data)) {
                Debug.LogError("Fatal error during data cleanup, go fix your json");
                return false;
            }

            int n_of_bytes_per_atom = 0;   //number of bytes that atom attributes take per atom
            int n_of_atomAttributes = data.atomAttributes.Length;
            int n_of_pathAttributes = data.pathAttributes.Length;

            for (int i = 0; i < n_of_atomAttributes; i++) {
                if (data.atomAttributes[i].type == IPCReceiver.LoadingData.DataType.float32 || data.atomAttributes[i].type == IPCReceiver.LoadingData.DataType.int32)
                    n_of_bytes_per_atom += 4;
                else
                    n_of_bytes_per_atom += 8;
            }

            float AllTimeMinimumOfColorAttribute = float.PositiveInfinity;
            float AllTimeMaximumOfColorAttribute = float.NegativeInfinity;

            float ReadAttribute_f(BinaryReader reader, IPCReceiver.LoadingData.DataType type) {
                switch (type) {
                    case IPCReceiver.LoadingData.DataType.float32:
                        return reader.ReadSingle();
                    case IPCReceiver.LoadingData.DataType.float64:
                        return (float)reader.ReadDouble();
                    case IPCReceiver.LoadingData.DataType.int32:
                        return (float)reader.ReadInt32();
                    case IPCReceiver.LoadingData.DataType.int64:
                        return (float)reader.ReadInt64();
                    default: //Never happens.
                        return 0f;
                }
            }

            int ReadAttribute_i(BinaryReader reader, IPCReceiver.LoadingData.DataType type) {
                switch (type) {
                    case IPCReceiver.LoadingData.DataType.float32:
                        return (int)reader.ReadSingle();
                    case IPCReceiver.LoadingData.DataType.float64:
                        return (int)reader.ReadDouble();
                    case IPCReceiver.LoadingData.DataType.int32:
                        return reader.ReadInt32();
                    case IPCReceiver.LoadingData.DataType.int64:
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
                startColor = LDColor_to_Color(data.atomAttributes[Color_RoleIndex].colorStart);
                endColor = LDColor_to_Color(data.atomAttributes[Color_RoleIndex].colorEnd);
            }

            Vector3 lowerTruncature = LDVector3_to_Vector3(data.lowerTruncature);
            Vector3 upperTruncature = LDVector3_to_Vector3(data.upperTruncature);

            if (data.useGPSCoords)
                Tools.SetGPSOrigin(LDVector2_to_Vector2(data.GPSOrigin));

            if (data.allPaths) {
                data.randomPaths = false;
                data.chosen_n_paths = data.dataset_n_paths;
                data.chosen_paths_start = 0;
                data.chosen_paths_end = data.dataset_n_paths;
                data.chosen_paths_step = 1;
            }

            int[] keptPaths;
            if (data.randomPaths) {
                keptPaths = new int[data.chosen_n_paths];
            }
            else {
                keptPaths = new int[(data.chosen_paths_end - data.chosen_paths_start) / data.chosen_paths_step];
            }

            if (data.randomPaths) {
                SortedSet<int> chosenRandomPaths = new SortedSet<int>(); // SortedSet because keptPaths should always be sorted
                System.Random rnd = new System.Random();
                for (int i = 0; i < keptPaths.Length; i++) {
                    while (!chosenRandomPaths.Add(rnd.Next(data.chosen_paths_start, data.chosen_paths_end))) { }
                }
                chosenRandomPaths.CopyTo(keptPaths);
            }
            else {
                for (int i = 0; i < keptPaths.Length; i++) {
                    keptPaths[i] = data.chosen_paths_start + i * data.chosen_paths_step;
                }
            }

            paths = new List<GlobalPath>(keptPaths.Length);
            Color32[] pathColors = new Color32[keptPaths.Length];
            for (int i = 0; i < keptPaths.Length; i++)
                pathColors[i] = UnityEngine.Random.ColorHSV();

            Tools.AddClockStop("Generated paths array");

            // Load Assets Bundles
            int n_of_assetBundles = data.assetBundles.Length;
            for (int i = 0; i < n_of_assetBundles; i++) {
    
                AssetBundle ab = AssetBundle.LoadFromFile(GetFullPath(data.assetBundles[i].filename));
                if (ab == null) {
                    Debug.LogWarning("Failed to load AssetBundle " + data.assetBundles[i].name);
                    continue;
                }

                GameObject[] prefabs = ab.LoadAllAssets<GameObject>();
                
                foreach (GameObject prefab in prefabs) {
                    if (data.assetBundles[i].overrideBundleTransform) {
                        prefab.transform.position = LDVector3_to_Vector3(data.assetBundles[i].position);
                        prefab.transform.eulerAngles = LDVector3_to_Vector3(data.assetBundles[i].rotation);
                        prefab.transform.localScale = LDVector3_to_Vector3(data.assetBundles[i].scale);
                    }
                    Instantiate(prefab);
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

            if (data.allInstants) {
                data.chosen_instants_start = 0;
                data.chosen_instants_end = data.dataset_n_instants;
                data.chosen_instants_step = 1;
            }

            int fileStart = 0;
            int fileEnd = 1;

            if (data.severalFiles_splitInstants) {
                fileStart = data.chosen_instants_start / data.splitInstants_instantsPerFile;
                fileEnd = (data.chosen_instants_end - 1) / data.splitInstants_instantsPerFile + 1;
            }

            BinaryReader br = null;

            for (int i_file = fileStart; i_file < fileEnd; i_file++) {
                string currentFileName;

                if (data.severalFiles_splitInstants) {
                    currentFileName = GetFullPath(GetCompositeFilename(data.filename, data.severalFiles_firstFileSuffix, i_file));
                }
                else {
                    currentFileName = GetFullPath(data.filename);
                }

                if (br != null)
                    br.Close();
                try {
                    if (data.endianness == IPCReceiver.LoadingData.Endianness.big) {
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

                    GlobalPath p;
                    if (i_file == fileStart) {
                        GameObject go;
                        go = new GameObject(pathID.ToString());
                        go.transform.parent = transform;
                        p = go.AddComponent<GlobalPath>();
                        p.atoms = new List<GlobalAtom>();
                        if (!data.severalFiles_splitInstants)
                            p.atoms.Capacity = Math.Min((data.chosen_instants_end - data.chosen_instants_start) / data.chosen_instants_step, (readableInstants - data.chosen_instants_start) / data.chosen_instants_step);
                        paths.Add(p);
                    }
                    else {
                        p = paths[i_path];
                    }

                    long nextPathPosition = br.BaseStream.Position + readableInstants * n_of_bytes_per_atom;

                    int localInstant = 0;
                    if (i_file == fileStart) {
                        localInstant = data.chosen_instants_start - i_file * data.splitInstants_instantsPerFile;
                        br.BaseStream.Position += localInstant * n_of_bytes_per_atom;
                    }

                    int instantsToRead = readableInstants;
                    if (i_file == fileEnd - 1) {
                        instantsToRead = Math.Min(instantsToRead, data.chosen_instants_end - i_file * data.splitInstants_instantsPerFile);
                    }

                    while (localInstant < instantsToRead) {
                        GlobalAtom a = new GlobalAtom {
                            path = p,
                            indexInPath = localInstant
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
                            a.time = (float)(data.chosen_instants_start + localInstant * data.chosen_instants_step);

                        if (Color_RoleIndex != -1) {
                            a.colorValue = atomAttributeValuesBuffer[Color_RoleIndex];
                            if (data.atomAttributes[Color_RoleIndex].valueColorUseMinMax) {
                                AllTimeMinimumOfColorAttribute = Mathf.Min(AllTimeMinimumOfColorAttribute, a.colorValue);
                                AllTimeMaximumOfColorAttribute = Mathf.Max(AllTimeMaximumOfColorAttribute, a.colorValue);
                            }
                            else {
                                IPCReceiver.LoadingData.AtomAttribute attr = data.atomAttributes[Color_RoleIndex];
                                a.BaseColor = Color32.Lerp(startColor, endColor, (a.colorValue - attr.valueColorStart) / (attr.valueColorEnd - attr.valueColorStart));
                            }
                        }
                        else {
                            a.BaseColor = pathColors[i_path];
                        }

                        p.atoms.Add(a);

                        localInstant += data.chosen_instants_step;
                        br.BaseStream.Position += (data.chosen_instants_step - 1) * n_of_bytes_per_atom; //Skip atoms if necessary
                    }

                    br.BaseStream.Position = nextPathPosition;

                    currentPath++;
                }
            }

            if (Color_RoleIndex != -1 && data.atomAttributes[Color_RoleIndex].valueColorUseMinMax) {
                for (int j=0; j < paths.Count; j++) {
                    for (int i=0; i < paths[j].atoms.Count; i++) {
                        GlobalAtom a = paths[j].atoms[i];
                        a.BaseColor = Color32.Lerp(startColor, endColor, (a.colorValue - AllTimeMinimumOfColorAttribute) / (AllTimeMaximumOfColorAttribute - AllTimeMinimumOfColorAttribute));
                    }
                }
            }

            Tools.EndClock("Loaded paths");

            return true;
        }

        public class GlobalPath : TimePath {
            public List<GlobalAtom> atoms = new List<GlobalAtom>();
            public override IReadOnlyList<Atom> AtomsAsBase { get { return atoms; } }
            public override IReadOnlyList<TimeAtom> AtomsAsTime { get { return atoms; } }

        }

        public class GlobalAtom : TimeAtom {
            public float colorValue;
        }

        protected override float InterpretTime(string word) {
            return 0;
        }
    }
}
