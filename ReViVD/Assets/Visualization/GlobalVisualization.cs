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

        bool CleanupData(IPCReceiver.LoadingData data) { //Fixes potential errors in the .json (ensures end > start, n_ values positive, etc.)
        
            if (data.severalFiles) {
                return false; //TODO : review json to deal with several files, deal with n_instants per file
            }
            else {
                if (!File.Exists(data.filename)) {
                    Debug.LogError("Data file not found");
                    return false;
                }
            }


            void CheckValue<T>(ref T value, bool condition, T defaultvalue, string log) {
                if (condition) {
                    Debug.LogWarning(log + ", replacing with " + defaultvalue.ToString());
                    value = defaultvalue;
                }
            }

            CheckValue(ref data.districtSize.x, data.districtSize.x <= 0, 20, "Negative X value in District Size");
            CheckValue(ref data.districtSize.y, data.districtSize.y <= 0, 20, "Negative Y value in District Size");
            CheckValue(ref data.districtSize.z, data.districtSize.z <= 0, 20, "Negative Z value in District Size");

            CheckValue(ref data.lowerTruncature.x, data.lowerTruncature.x > data.upperTruncature.x, -1000, "X value of lowerTruncature is bigger than the corresponding value of upperTruncature");
            CheckValue(ref data.lowerTruncature.y, data.lowerTruncature.y > data.upperTruncature.y, -1000, "Y value of lowerTruncature is bigger than the corresponding value of upperTruncature");
            CheckValue(ref data.lowerTruncature.z, data.lowerTruncature.z > data.upperTruncature.z, -1000, "Z value of lowerTruncature is bigger than the corresponding value of upperTruncature");

            CheckValue(ref data.upperTruncature.x, data.lowerTruncature.x > data.upperTruncature.x, 1000, "X value of lowerTruncature is bigger than the corresponding value of upperTruncature");
            CheckValue(ref data.upperTruncature.y, data.lowerTruncature.y > data.upperTruncature.y, 1000, "Y value of lowerTruncature is bigger than the corresponding value of upperTruncature");
            CheckValue(ref data.upperTruncature.z, data.lowerTruncature.z > data.upperTruncature.z, 1000, "Z value of lowerTruncature is bigger than the corresponding value of upperTruncature");

            if (data.file_n_paths <= 0) {
                Debug.LogError("Negative number of paths");
            }
            else if (!data.allPaths) {
                CheckValue(ref data.chosen_paths_start, data.chosen_paths_start < 0, 0, "Negative value for chosen_paths_start");
                CheckValue(ref data.chosen_paths_start, data.chosen_paths_start > data.file_n_paths, 0, "Chosen_paths_start value bigger than number of paths");

                CheckValue(ref data.chosen_paths_end, data.chosen_paths_end < 0, 500, "Negative value for chosen_paths_end");
                CheckValue(ref data.chosen_paths_end, data.chosen_paths_end > data.file_n_paths, 500, "Chosen paths end bigger than number of paths");

                CheckValue(ref data.chosen_paths_start, data.chosen_paths_start > data.chosen_paths_end, 0, "Chosen paths start bigger than chosen paths end");
                CheckValue(ref data.chosen_paths_end, data.chosen_paths_start > data.chosen_paths_end, 500, "Chosen paths start bigger than chosen paths end");


                CheckValue(ref data.chosen_paths_step, data.chosen_paths_step < 1, 1, "Incorrect value for chosen_paths_step");

                if (data.randomPaths) {
                    CheckValue(ref data.chosen_n_paths, data.chosen_n_paths <= 0, 500, "Negative value for chosen_n_paths");
                    CheckValue(ref data.chosen_n_paths, data.chosen_n_paths > data.file_n_paths, 500, "Chosen_n_paths value bigger than number of paths");
                }
            }
           
            //constant_n_instants

            if (data.file_n_instants <= 0) {
                Debug.LogError("Negative number of instants");
            }
            else if (!data.allInstants) {
                CheckValue(ref data.chosen_instants_start, data.chosen_instants_start < 0, 0, "Negative value for chosen_instants_start");
                CheckValue(ref data.chosen_instants_start, data.chosen_instants_start > data.file_n_instants, 0, "Chosen_instants_start value bigger than number of instants");

                CheckValue(ref data.chosen_instants_end, data.chosen_instants_end < 0, data.file_n_instants, "Negative value for chosen_instants_end");
                CheckValue(ref data.chosen_instants_end, data.chosen_instants_end > data.file_n_instants, data.file_n_instants, "Chosen instants end bigger than number of instants");

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
                return false;
            }
            if (data.pathAttributeUsedAs_n_atoms != "" && !CheckIfPathAttributeExists(data.pathAttributeUsedAs_n_atoms, data.pathAttributes)) {
                Debug.LogError("The path attribute to use as n_atoms doesn't exist");
                return false;
            }

            //atomAttributes
            bool CheckIfAtomAttributeExists(string attribute, IPCReceiver.LoadingData.AtomAttribute[] atomAttributes) {
                for (int i = 0; i < atomAttributes.Length; i++) {
                    if (atomAttributes[i].name == attribute)
                        return true;
                }
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

                if (data.atomAttributes[i].valueColorEnd < data.atomAttributes[i].valueColorStart) {
                    Debug.LogWarning("valueColorStart is bigger than valuecolorEnd for atom attribute " + data.atomAttributes[i].name);
                    float temp = data.atomAttributes[i].valueColorEnd;
                    data.atomAttributes[i].valueColorEnd = data.atomAttributes[i].valueColorStart;
                    data.atomAttributes[i].valueColorStart = temp;
                }
            }

            return true;
        }

        IPCReceiver.LoadingData data;
        public bool manualLoading = false;
        public string jsonPath = "";
        protected override bool LoadFromFile() {
            Tools.StartClock();
            
            if (!manualLoading) {
                IPCReceiver.Instance.CatchData();
                data = IPCReceiver.Instance.data;
                Tools.AddClockStop("Received json data");
            }
            else {
                StreamReader reader = new StreamReader(jsonPath);
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
                data.chosen_n_paths = data.file_n_paths;
                data.chosen_paths_start = 0;
                data.chosen_paths_end = data.file_n_paths;
                data.chosen_paths_step = 1;
            }

            int final_n_paths = 0; //Number of paths that will be loaded in fine
            if (data.randomPaths) {
                final_n_paths = data.chosen_n_paths;
            }
            else {
                final_n_paths = (data.chosen_paths_end - data.chosen_paths_start) / data.chosen_paths_step;
            }
            
            int[] keptPaths = new int[final_n_paths];

            if (data.randomPaths) {
                SortedSet<int> chosenRandomPaths = new SortedSet<int>(); // SortedSet because keptPaths should always be sorted
                System.Random rnd = new System.Random();
                for (int i = 0; i < final_n_paths; i++) {
                    while (!chosenRandomPaths.Add(rnd.Next(data.chosen_paths_start, data.chosen_paths_end))) { }
                }
                chosenRandomPaths.CopyTo(keptPaths);
            }
            else {
                for (int i = 0; i < final_n_paths; i++) {
                    keptPaths[i] = data.chosen_paths_start + i * data.chosen_paths_step;
                }
            }

            paths = new List<GlobalPath>(final_n_paths); // ok

            Tools.AddClockStop("Generated paths array");


            // Load Assets Bundles
            int n_of_assetBundles = data.assetBundles.Length;
            for (int i = 0; i< n_of_assetBundles; i++) {
                var myLoadedAssetBundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(Application.streamingAssetsPath, data.assetBundles[i].filename));
                if (myLoadedAssetBundle == null) {
                    Debug.LogWarning("Failed to load AssetBundle " + data.assetBundles[i].name);
                    continue;
                }
                var prefab = myLoadedAssetBundle.LoadAsset<GameObject>(data.assetBundles[i].name);

                if (data.assetBundles[i].overrideBundleTransform) {
                    prefab.transform.position = LDVector3_to_Vector3(data.assetBundles[i].position);
                    prefab.transform.eulerAngles = LDVector3_to_Vector3(data.assetBundles[i].rotation);
                    prefab.transform.localScale = LDVector3_to_Vector3(data.assetBundles[i].scale);
                }
                Instantiate(prefab);
                myLoadedAssetBundle.Unload(false);
            }
            Tools.AddClockStop("Loaded assetBundles");

            string currentFileName = data.filename;
            if (data.severalFiles) {
                //TODO: starting filename for severalFiles
            }

            BinaryReader br;
            if (data.endianness == IPCReceiver.LoadingData.Endianness.big) {
                br = new BinaryReader_BigEndian(File.Open(data.filename, FileMode.Open)); 
            }
            else {
                br = new BinaryReader(File.Open(data.filename, FileMode.Open)); 
            }
            Tools.AddClockStop("Loaded data file");
            
            int currentPath = 0;
            for (int i = 0; i < final_n_paths; i++) { // ok
                if (br.BaseStream.Position >= br.BaseStream.Length) {
                    Debug.LogError("Reached EoF on loading paths after " + paths.Count + " paths");
                    break;
                }

                if (data.severalFiles && false) {
                    //TODO : switch to new file if necessary
                }

                int pathLength = 0;
                int pathID = 0;
                    
                void ReadPathAttributes() {
                    //Default values
                    pathLength = data.file_n_instants;
                    pathID = keptPaths[i];

                    for (int j = 0; j < n_of_pathAttributes; j++) {
                        if (j == N_RoleIndex || j == ID_RoleIndex) {
                            int attributeValue = ReadAttribute_i(br, data.pathAttributes[j].type);

                            if (j == N_RoleIndex)
                                pathLength = attributeValue;
                            if (j == ID_RoleIndex)
                                pathID = attributeValue;
                        }
                        else {
                            ReadAttribute_f(br, data.pathAttributes[j].type);
                        }
                    }
                }

                ReadPathAttributes();
                if (data.allInstants) {
                    data.chosen_instants_start = 0;
                    data.chosen_instants_end = pathLength;
                    data.chosen_instants_step = 1;
                }

                while (currentPath < keptPaths[i]) {
                    br.BaseStream.Position += pathLength * n_of_bytes_per_atom;
                    ReadPathAttributes();
                    currentPath++;
                }

                if (data.chosen_instants_start + data.chosen_instants_step >= pathLength) //Don't bother if the path is not long enough to have at least two atoms in it
                    continue;

                int final_n_instants = Math.Min((data.chosen_instants_end - data.chosen_instants_start) / data.chosen_instants_step, (pathLength - data.chosen_instants_start) / data.chosen_instants_step);
                //Number of instants that will be loaded in fine

                GameObject go;
                go = new GameObject(pathID.ToString());
                go.transform.parent = transform;
                GlobalPath p = go.AddComponent<GlobalPath>();
                p.atoms = new List<GlobalAtom>(final_n_instants);
                
                Color32 pathColor = UnityEngine.Random.ColorHSV(); //Used if no atom attribute is used for coloring

                long nextPathPosition = br.BaseStream.Position + pathLength * n_of_bytes_per_atom;
                br.BaseStream.Position += data.chosen_instants_start * n_of_bytes_per_atom;

                for (int j = 0; j < final_n_instants; j++) {
                    GlobalAtom a = new GlobalAtom {
                        path = p,
                        indexInPath = j
                    };

                    for (int k = 0; k < n_of_atomAttributes; k++) {
                        atomAttributeValuesBuffer[k] = ReadAttribute_f(br, data.atomAttributes[k].type); 
                    }

                    if (data.useGPSCoords) {
                        if (X_RoleIndex != -1 && Z_RoleIndex != -1) {
                            a.point = Tools.GPSToXYZ(new Vector2(atomAttributeValuesBuffer[X_RoleIndex], atomAttributeValuesBuffer[Z_RoleIndex]));
                            a.point.x *= data.atomAttributes[X_RoleIndex].sizeCoeff;
                            a.point.z *= data.atomAttributes[Z_RoleIndex].sizeCoeff;
                        }
                    }
                    else {
                        if (X_RoleIndex != -1)
                            a.point.x = atomAttributeValuesBuffer[X_RoleIndex] * data.atomAttributes[X_RoleIndex].sizeCoeff;
                        if (Z_RoleIndex != -1)
                            a.point.z = atomAttributeValuesBuffer[Z_RoleIndex] * data.atomAttributes[Z_RoleIndex].sizeCoeff;
                    }
                    if (Y_RoleIndex != -1)
                        a.point.y = atomAttributeValuesBuffer[Y_RoleIndex] * data.atomAttributes[Y_RoleIndex].sizeCoeff;

                    a.point = Vector3.Max(a.point, lowerTruncature);
                    a.point = Vector3.Min(a.point, upperTruncature);

                    if (T_RoleIndex != -1)
                        a.time = atomAttributeValuesBuffer[T_RoleIndex];
                    else
                        a.time = (float)(data.chosen_instants_start + j * data.chosen_instants_step);

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
                        a.BaseColor = pathColor;
                    }

                    p.atoms.Add(a);

                    br.BaseStream.Position += (data.chosen_instants_step - 1) * n_of_bytes_per_atom; //Skips atoms if necessary
                }

                br.BaseStream.Position = nextPathPosition;

                paths.Add(p);
                currentPath++;
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
