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

        private Vector3 Vector3dToVector3(IPCReceiver.LoadingData.Vector3D vector3D) {
            return new Vector3(vector3D.x, vector3D.y, vector3D.z);
        }
        private Vector2 Vector2dToVector2(IPCReceiver.LoadingData.Vector2D vector2D) {
            return new Vector2(vector2D.x, vector2D.y);
        }
        private Color LoadingDataColorToColor(IPCReceiver.LoadingData.Color color) {
            if ((int)color == 0)
                return Color.red;
            else if ((int)color == 1)
                return Color.green;
            else
                return Color.blue;
        }

        public void Reset() {
            districtSize = Vector3dToVector3(data.districtSize);
        }

        private enum PathAttributeRole {
            Length, ID, Other
        }

        private enum AtomAttributeRole {
            X, Y, Z, T, Color, Other
        }

        private bool Is32(IPCReceiver.LoadingData.DataType type) {
            return (type == IPCReceiver.LoadingData.DataType.float32) || (type == IPCReceiver.LoadingData.DataType.int32);
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


            void CheckFloatValue(float value, bool condition, float defaultvalue, string log) {
                if (condition) {
                    Debug.LogWarning(log);
                    value = defaultvalue;
                }
            }

            void CheckIntValue(int value, bool condition, int defaultvalue, string log) {
                if (condition) {
                    Debug.LogWarning(log);
                    value = defaultvalue;
                }
            }

            CheckFloatValue(data.districtSize.x, data.districtSize.x <= 0, 20, "Negative X value in District Size");
            CheckFloatValue(data.districtSize.y, data.districtSize.y <= 0, 20, "Negative Y value in District Size");
            CheckFloatValue(data.districtSize.z, data.districtSize.z <= 0, 20, "Negative Z value in District Size");

            CheckFloatValue(data.lowerTruncature.x, data.lowerTruncature.x > data.upperTruncature.x, -1000, "X value of lowerTruncature is bigger than the corresponding value of upperTruncature");
            CheckFloatValue(data.lowerTruncature.y, data.lowerTruncature.y > data.upperTruncature.y, -1000, "Y value of lowerTruncature is bigger than the corresponding value of upperTruncature");
            CheckFloatValue(data.lowerTruncature.z, data.lowerTruncature.z > data.upperTruncature.z, -1000, "Z value of lowerTruncature is bigger than the corresponding value of upperTruncature");

            CheckFloatValue(data.upperTruncature.x, data.lowerTruncature.x > data.upperTruncature.x, 1000, "X value of lowerTruncature is bigger than the corresponding value of upperTruncature");
            CheckFloatValue(data.upperTruncature.y, data.lowerTruncature.y > data.upperTruncature.y, 1000, "Y value of lowerTruncature is bigger than the corresponding value of upperTruncature");
            CheckFloatValue(data.upperTruncature.z, data.lowerTruncature.z > data.upperTruncature.z, 1000, "Z value of lowerTruncature is bigger than the corresponding value of upperTruncature");

            if (data.file_n_paths <= 0) {
                Debug.LogError("Negative number of paths");
                return false;
            }

            //randomColorPaths

            if (!data.allPaths) {
                CheckIntValue(data.chosen_paths_start, data.chosen_paths_start < 0, 0, "Negative value for chosen_paths_start");
                CheckIntValue(data.chosen_paths_end, data.chosen_paths_end <= data.file_n_paths, 500, "Chosen paths end bigger than number of paths");
                CheckIntValue(data.chosen_paths_step, data.chosen_paths_step < 1, 1, "Incorrect value for chosen_paths_step");

                if (data.randomPaths) {
                    CheckIntValue(data.chosen_n_paths, data.chosen_n_paths <= 0, 500, "Negative value for chosen_n_paths");
                }
            }
           
            //constant_n_instants

            if (data.file_n_instants <= 0) {
                Debug.LogError("Negative number of instants");
                return false;
            }
            if (!data.allInstants) {
                CheckIntValue(data.chosen_instants_start, data.chosen_instants_start < 0, 0, "Negative value for chosen_instants_start");
                CheckIntValue(data.chosen_instants_end, data.chosen_instants_end <= data.file_n_instants, 200, "Chosen instants end bigger than number of instants");
                CheckIntValue(data.chosen_instants_step, data.chosen_instants_step < 1, 2, "Incorrect value for chosen_instants_step");
            }

            if (data.useGPSCoords) {
                CheckFloatValue(data.GPSOrigin.x, Mathf.Abs(data.GPSOrigin.x) > 90, 0, "latitude in decimal degree out of range +-90°");
                CheckFloatValue(data.GPSOrigin.y, Mathf.Abs(data.GPSOrigin.y) > 180, 0, "longitude in decimal degree out of range +-90°");
            }

            CheckFloatValue(data.spheresRadius, data.spheresRadius < 0, 2, "Negative Value for spheresRadius");
            CheckFloatValue(data.spheresAnimSpeed, data.spheresAnimSpeed < 0, 1, "Negative Value for spheresAnimSpeed");

            // spheresGlobalTime --> can either be negative or positive
            // spheresDisplay
            //assetBundles --> we already check if file exists while opening it

            //pathAttributes
            bool CheckIfPathAttributeExists(string attribute, IPCReceiver.LoadingData.PathAttribute[] pathAttributes) {
                bool knownPathAttribute = false;
                for (int i = 0; i < pathAttributes.Length; i++) {
                    if (pathAttributes[i].name == attribute)
                        knownPathAttribute = true;
                }
                return knownPathAttribute;
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
                bool knownAtomAttribute = false;
                for (int i = 0; i < atomAttributes.Length; i++) {
                    if (atomAttributes[i].name == attribute)
                        knownAtomAttribute = true;
                }
                return knownAtomAttribute;
            }

            if (data.atomAttributeUsedAs_x != "" && !CheckIfAtomAttributeExists(data.atomAttributeUsedAs_x, data.atomAttributes)) {
                Debug.LogError("The atom attribute to use as x doesn't exist");
                return false;
            }
            if (data.atomAttributeUsedAs_y != "" && !CheckIfAtomAttributeExists(data.atomAttributeUsedAs_y, data.atomAttributes)) {
                Debug.LogError("The atom attribute to use as y doesn't exist");
                return false;
            }
            if (data.atomAttributeUsedAs_z != "" && !CheckIfAtomAttributeExists(data.atomAttributeUsedAs_z, data.atomAttributes)) {
                Debug.LogError("The atom attribute to use as z doesn't exist");
                return false;
            }
            if (data.atomAttributeUsedAs_t != "" && !CheckIfAtomAttributeExists(data.atomAttributeUsedAs_t, data.atomAttributes)) {
                Debug.LogError("The atom attribute to use as t doesn't exist");
                return false;
            }
            if (data.atomAttributeUsedAs_color != "" && !CheckIfAtomAttributeExists(data.atomAttributeUsedAs_color, data.atomAttributes)) {
                Debug.LogError("The atom attribute to use as color doesn't exist");
                return false;
            }

            for (int i=0; i < data.atomAttributes.Length; i++) {
                CheckFloatValue(data.atomAttributes[i].sizeCoeff, data.atomAttributes[i].sizeCoeff < 0, 1, "One size coeff of an atom attribute is negative");
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

            if (!CleanupData(data))
                return false;

            Vector3 lowerTruncature = Vector3dToVector3(data.lowerTruncature);
            Vector3 upperTruncature = Vector3dToVector3(data.upperTruncature);

            int n_of_bytes_per_atom = 0;   //number of bytes that atom attributes take per atom
            int n_of_atomAttributes = data.atomAttributes.Length;
            int n_of_pathAttributes = data.pathAttributes.Length;

            for (int i = 0; i < n_of_atomAttributes; i++) {
                if (Is32(data.atomAttributes[i].type))
                    n_of_bytes_per_atom += 4;
                else
                    n_of_bytes_per_atom += 8;
            }

            Dictionary<string, int> attributes_position_for_atoms = new Dictionary<string, int>();
            Dictionary<string, int> attributes_position_for_paths = new Dictionary<string, int>();

            PathAttributeRole[] PathAttributesRoleOrder = new PathAttributeRole[n_of_pathAttributes];
            AtomAttributeRole[] AtomAttributesRoleOrder = new AtomAttributeRole[n_of_atomAttributes];
            bool pathIDinData = false;

            for (int i = 0; i < n_of_pathAttributes; i++) {
                attributes_position_for_paths.Add(data.pathAttributes[i].name, i);
                if (data.pathAttributes[i].name == data.pathAttributeUsedAs_id) {
                    PathAttributesRoleOrder[i] = PathAttributeRole.ID;
                    pathIDinData = true;
                }
                else if (data.pathAttributes[i].name == data.pathAttributeUsedAs_n_atoms) {
                    PathAttributesRoleOrder[i] = PathAttributeRole.Length;
                }
                else {
                    PathAttributesRoleOrder[i] = PathAttributeRole.Other;
                }
            }

            float sizeCoeffX = 1;
            float sizeCoeffY = 1;
            float sizeCoeffZ = 1;
            bool timeInData = false;
            bool colorInData = false;
            Color startColor = Color.blue;
            Color endColor = Color.red;

            for (int i=0; i < n_of_atomAttributes; i++) {
                attributes_position_for_atoms.Add(data.atomAttributes[i].name, i);
                if (data.atomAttributes[i].name == data.atomAttributeUsedAs_x) {
                    AtomAttributesRoleOrder[i] = AtomAttributeRole.X;
                    sizeCoeffX = data.atomAttributes[i].sizeCoeff;
                }
                else if (data.atomAttributes[i].name == data.atomAttributeUsedAs_y) {
                    AtomAttributesRoleOrder[i] = AtomAttributeRole.Y;
                    sizeCoeffY = data.atomAttributes[i].sizeCoeff;
                }
                else if (data.atomAttributes[i].name == data.atomAttributeUsedAs_z) {
                    AtomAttributesRoleOrder[i] = AtomAttributeRole.Z;
                    sizeCoeffZ = data.atomAttributes[i].sizeCoeff;
                }
                else if (data.atomAttributes[i].name == data.atomAttributeUsedAs_t) {
                    AtomAttributesRoleOrder[i] = AtomAttributeRole.T;
                    timeInData = true;
                }
                else if (data.atomAttributes[i].name == data.atomAttributeUsedAs_color) {
                    AtomAttributesRoleOrder[i] = AtomAttributeRole.Color;
                    colorInData = true;
                    startColor = LoadingDataColorToColor(data.atomAttributes[i].colorStart);
                    endColor = LoadingDataColorToColor(data.atomAttributes[i].colorEnd);
                }
                else {
                    AtomAttributesRoleOrder[i] = AtomAttributeRole.Other;
                }
            }

           
            int chosen_n_paths = (data.allPaths) ? data.file_n_paths : (data.chosen_paths_end - data.chosen_paths_start) /chosen_path_step; // int division
            int chosen_n_instants = (data.allInstants) ? data.file_n_instants : (data.chosen_instants_end - data.chosen_instants_start) / chosen_instant_step; // int division
            int[] keptPaths = new int[chosen_n_paths]; // ok

            if (data.randomPaths) { // ok
                SortedSet<int> chosenRandomPaths = new SortedSet<int>(); // because keptPaths should always be sorted
                System.Random rnd = new System.Random();
                for (int i = 0; i < data.chosen_n_paths; i++) {// todo change chosen_n_paths
                    while (!chosenRandomPaths.Add(rnd.Next(data.chosen_paths_start, data.chosen_paths_end))) { }
                }
                chosenRandomPaths.CopyTo(keptPaths);
            }
            else { //ok
                for (int i = 0; i < chosen_n_paths && data.chosen_paths_start + chosen_path_step * i < data.file_n_paths; i++)
                    keptPaths[i] = data.chosen_paths_start + chosen_path_step * i;
            }
            Tools.AddClockStop("generated paths array");

            paths = new List<GlobalPath>(chosen_n_paths); // ok


            // Load Assets Bundles
            int n_of_assetBundles = data.assetBundles.Length;
            for (int i = 0; i< n_of_assetBundles; i++) {
                var myLoadedAssetBundle = AssetBundle.LoadFromFile(System.IO.Path.Combine(Application.streamingAssetsPath, data.assetBundles[i].filename));
                if (myLoadedAssetBundle == null) {
                    Debug.Log("Failed to load AssetBundle!");
                    break;
                }
                var prefab = myLoadedAssetBundle.LoadAsset<GameObject>(data.assetBundles[i].name);

                if (data.assetBundles[i].overrideBundleTransform) {
                    prefab.transform.position = Vector3dToVector3(data.assetBundles[i].position);
                    prefab.transform.eulerAngles = Vector3dToVector3(data.assetBundles[i].rotation);
                    prefab.transform.localScale = Vector3dToVector3(data.assetBundles[i].scale);
                }
                Instantiate(prefab);
                myLoadedAssetBundle.Unload(false);
            }
            Tools.AddClockStop("Loaded assetBundles");


            if (!data.severalFiles) { // if only one file so all visu except naso

                BinaryReader br; // br big-endian or little-endian
                if (data.endianness == IPCReceiver.LoadingData.Endianness.big) {
                    br = new BinaryReader_BigEndian(File.Open(data.filename, FileMode.Open)); 
                }
                else {
                    br = new BinaryReader(File.Open(data.filename, FileMode.Open)); 
                }
                Tools.AddClockStop("Loaded data file");

                float minColor = 100000;
                float maxColor = -100000;
                

                int currentPath = 0;
                for (int i = 0; i < keptPaths.Length; i++) { // ok
                    if (br.BaseStream.Position >= br.BaseStream.Length) {
                        Debug.Log("Reached EoF on loading paths after " + paths.Count + " paths");
                        break;
                    }

                    int pathLength = 0;
                    int pathID = 0;

                    float xAttribute = 0;
                    float yAttribute = 0;
                    float zAttribute = 0;
                    float tAttribute = 0;
                    float colorAttribute = 0;
                    
                    

                    /*float ReadFloat_p(IPCReceiver.LoadingData.PathAttribute attr) {
                        return is32(attr.type) ? br.ReadSingle() : (float)br.ReadDouble();
                    }*/

                    float ReadFloat_a(IPCReceiver.LoadingData.AtomAttribute attr) {
                        return Is32(attr.type) ? br.ReadSingle() : (float)br.ReadDouble();
                    }

                    int ReadInt_p(IPCReceiver.LoadingData.PathAttribute attr) {
                        return Is32(attr.type) ? br.ReadInt32() : (int)br.ReadInt64();
                    }

                    /*int ReadInt_a(IPCReceiver.LoadingData.AtomAttribute attr) {
                        return is32(attr.type) ? br.ReadInt32() : (int)br.ReadInt64();
                    }*/

                    void ReadPathAttributes() {
                        for (int j = 0; j < n_of_pathAttributes; j++) {
                            if (PathAttributesRoleOrder[j] == PathAttributeRole.ID) {
                                pathID = ReadInt_p(data.pathAttributes[j]);
                            }
                            else if (PathAttributesRoleOrder[j] == PathAttributeRole.Length) {
                                pathLength = ReadInt_p(data.pathAttributes[j]);
                            }
                            else {
                                br.BaseStream.Position += Is32(data.pathAttributes[j].type) ? 4 : 8; // in case that there are other path attributes
                            }
                        }
                    }

                    void ReadAtomAttributes() {
                        for (int k = 0; k < n_of_atomAttributes; k++) {
                            if (AtomAttributesRoleOrder[k] == AtomAttributeRole.X) {
                                xAttribute = ReadFloat_a(data.atomAttributes[k]);
                            }
                            else if (AtomAttributesRoleOrder[k] == AtomAttributeRole.Y) {
                                yAttribute = ReadFloat_a(data.atomAttributes[k]);
                            }
                            else if (AtomAttributesRoleOrder[k] == AtomAttributeRole.Z) {
                                zAttribute = ReadFloat_a(data.atomAttributes[k]);
                            }
                            else if (AtomAttributesRoleOrder[k] == AtomAttributeRole.T) {
                                tAttribute = ReadFloat_a(data.atomAttributes[k]);
                            }
                            else if (AtomAttributesRoleOrder[k] == AtomAttributeRole.Color) {
                                colorAttribute = ReadFloat_a(data.atomAttributes[k]);
                                minColor = Mathf.Min(minColor, colorAttribute);
                                maxColor = Mathf.Max(maxColor, colorAttribute);
                            }
                            else {
                                br.BaseStream.Position += Is32(data.atomAttributes[k].type) ? 4 : 8; // in case that there are other path attributes
                            }
                        }
                    }

                    ReadPathAttributes();

                    while (currentPath < keptPaths[i]) {
                        br.BaseStream.Position += pathLength * n_of_bytes_per_atom;
                        ReadPathAttributes();
                        currentPath++;
                    }

                    if (data.chosen_instants_start + chosen_instant_step >= pathLength) // ok
                        continue;
                    int true_n_instants = Math.Min(data.file_n_instants, pathLength - data.chosen_instants_start); // ok

                    GameObject go;
                    if (pathIDinData) { //if ID given in dataset
                        go = new GameObject(pathID.ToString());
                    }
                    else { //, otherwise, position of the path
                        go = new GameObject(keptPaths[i].ToString());
                    }
                    
                    go.transform.parent = transform;
                    GlobalPath p = go.AddComponent<GlobalPath>();
                    p.atoms = new List<GlobalAtom>(true_n_instants); // ok

                    Color32 color = UnityEngine.Random.ColorHSV();
                    if (data.randomColorPaths) {
                        color = UnityEngine.Random.ColorHSV(); // random color for the path
                    }

                    long nextPathPosition = br.BaseStream.Position + pathLength * n_of_bytes_per_atom; //ok
                    br.BaseStream.Position += data.chosen_instants_start * n_of_bytes_per_atom; //ok

                    for (int j = 0; j < true_n_instants; j += chosen_instant_step) {

                        Vector3 point = new Vector3();
                        if (data.useGPSCoords) {
                            Tools.SetGPSOrigin(Vector2dToVector2(data.GPSOrigin));

                            ReadAtomAttributes();
                            point = Tools.GPSToXYZ(new Vector2(xAttribute, zAttribute));
                            point.y = yAttribute;

                        }
                        else {
                            ReadAtomAttributes();
                            point.x = xAttribute;
                            point.y = yAttribute;
                            point.z = zAttribute;
                        }

                        //Tools.AddSubClockStop(point.x.ToString());
                        point.x *= sizeCoeffX;
                        point.y *= sizeCoeffY;
                        point.z *= sizeCoeffZ;

                        point = Vector3.Max(point, lowerTruncature); // ok
                        point = Vector3.Min(point, upperTruncature); // ok 

                        if (!timeInData) {
                            tAttribute = (float)(j + data.chosen_instants_start);
                        }

                        if (!colorInData) {
                            p.atoms.Add(new GlobalAtom() {
                                time = tAttribute,
                                point = point,
                                path = p,
                                indexInPath = j / chosen_instant_step,
                                BaseColor = color // randomColor of the Path
                            });
                        }
                        else {
                            p.atoms.Add(new GlobalAtom() {
                                time = tAttribute,
                                point = point,
                                path = p,
                                color = colorAttribute,
                                indexInPath = j / chosen_instant_step
                            });
                        }

                        br.BaseStream.Position += (chosen_instant_step - 1) * n_of_bytes_per_atom;//ok
                    }

                    br.BaseStream.Position = nextPathPosition;//ok

                    paths.Add(p);
                    currentPath++;
                }

                if (colorInData) {
                    for (int j=0; j < paths.Count; j++) {
                        for (int i=0; i < paths[j].atoms.Count; i++) {
                            GlobalAtom a = paths[j].atoms[i];
                            a.BaseColor = Color32.Lerp(startColor, endColor, (a.color - minColor) / (maxColor - minColor));
                        }
                    }
                }

                Tools.EndClock("Loaded paths");

                Debug.Log(paths.Count);

                return true;
            }
            else {
                // if several files  as naso visualisation
                return false;
            }
        }

        public class GlobalPath : TimePath {
            public List<GlobalAtom> atoms = new List<GlobalAtom>();
            public override IReadOnlyList<Atom> AtomsAsBase { get { return atoms; } }
            public override IReadOnlyList<TimeAtom> AtomsAsTime { get { return atoms; } }

        }

        public class GlobalAtom : TimeAtom {
            public float color;
        }

        private void Start() {
 
        }


        protected override float InterpretTime(string word) {
            return 0;
        }
    }
}
