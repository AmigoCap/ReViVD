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

        void CleanupData(IPCReceiver.LoadingData data) { //Fixes potential errors in the .json (ensures end > start, n_ values positive, etc.)

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
            CleanupData(data);

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
                Tools.AddSubClockStop(p.atoms.Count.ToString());

                br.BaseStream.Position = nextPathPosition;

                paths.Add(p);
                currentPath++;
            }

            if (Color_RoleIndex != -1 && data.atomAttributes[Color_RoleIndex].valueColorUseMinMax) {
                for (int j=0; j < paths.Count; j++) {
                    for (int i=0; i < paths[j].atoms.Count; i++) {
                        GlobalAtom a = paths[j].atoms[i];
                        a.BaseColor = Color32.Lerp(startColor, endColor, (a.colorValue - AllTimeMinimumOfColorAttribute) / (AllTimeMinimumOfColorAttribute - AllTimeMinimumOfColorAttribute));
                    }
                }
            }

            Tools.EndClock("Loaded paths");

            Debug.Log(paths.Count);

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
