using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

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

            string pathAttributeUsedAs_id = "id";
            string pathAttributeUsedAs_n_atoms = "n_atoms";

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

            string atomAttributeUsedAs_x = "x";
            string atomAttributeUsedAs_y = "y";
            string atomAttributeUsedAs_z = "z";
            string atomAttributeUsedAs_t = "t";

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
            public bool all_paths_to_display = true;
            public bool all_instants_to_display = true;
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
        public LoadingData data;

        private Vector3 Vector3dToVector3(LoadingData.Vector3D vector3D) {
            return new Vector3(vector3D.x, vector3D.y, vector3D.z);
        }
        private Vector2 Vector2dToVector2(LoadingData.Vector2D vector2D) {
            return new Vector2(vector2D.x, vector2D.y);
        }

        public void Reset() {
            districtSize = Vector3dToVector3(data.districtSize);
        }

        protected override bool LoadFromFile() {
            Tools.StartClock();
            Vector3 lowerTruncature = Vector3dToVector3(data.lowerTruncature); // ok 
            Vector3 upperTruncature = Vector3dToVector3(data.upperTruncature); // ok 

            int n_of_bytes_per_atom = 0;   //number of bytes that x, y, z ... attributes take per atom
            int n_of_atom_attributes = data.atomAttributes.Length;
            int n_of_path_attributes = data.pathAttributes.Length;

            for (int i = 0; i < n_of_atom_attributes; i++) { //ok
                if (data.atomAttributes[i].type == LoadingData.DataType.int32 || data.atomAttributes[i].type == LoadingData.DataType.float32)
                    n_of_bytes_per_atom += 4;
                else
                    n_of_bytes_per_atom += 8;
            }

            int chosen_n_paths = (data.all_paths_to_display) ? data.file_n_paths : (data.chosen_paths_end - data.chosen_paths_start + 1) / data.chosen_paths_step; // int division
            int chosen_n_instants = (data.all_instants_to_display) ? data.file_n_instants : (data.chosen_instants_end - data.chosen_instants_start + 1) / data.chosen_instants_step; // int division

            int[] keptPaths = new int[chosen_n_paths]; // ok
            int chosen_instant_step = Math.Max(data.chosen_instants_step, 1); // ok

            if (data.randomPaths) { // ok
                SortedSet<int> chosenRandomPaths = new SortedSet<int>(); //keptPaths should always be sorted
                System.Random rnd = new System.Random();
                for (int i = 0; i < chosen_n_paths; i++) {
                    while (!chosenRandomPaths.Add(rnd.Next(data.file_n_paths))) { }
                }
                chosenRandomPaths.CopyTo(keptPaths);
            }
            else { //ok
                for (int i = 0; i < chosen_n_paths && data.chosen_paths_start + data.chosen_paths_step * i < data.file_n_paths; i++)
                    keptPaths[i] = data.chosen_paths_start + data.chosen_paths_step * i;
            }
            Tools.AddClockStop("generated paths array");

            paths = new List<GlobalPath>(chosen_n_paths); // ok

            // deal with asset bundles here

            if (!data.severalFiles) { // if only one file so all visu except naso
                // if big endian / little endian

                BinaryReader br = new BinaryReader(File.Open(data.filename, FileMode.Open)); // /!\ only works for Little endian now
                Tools.AddClockStop("Loaded data file");

                int currentPath = 0;
                for (int i = 0; i < keptPaths.Length; i++) { // ok
                    if (br.BaseStream.Position == br.BaseStream.Length) {
                        Debug.Log("Reached EoF on loading paths after " + paths.Count + " paths");
                        break;
                    }

                    int pathLength = br.ReadInt32(); // path attribute to check, not always in the dataset
                    // read id if there is one

                    while (currentPath < keptPaths[i]) {
                        br.BaseStream.Position += pathLength * 3 * 4; // change 3 and 4 --> different selon visu
                        pathLength = br.ReadInt32();
                        // read id if there is one
                        currentPath++;
                    }

                    if (data.chosen_instants_start + chosen_instant_step >= pathLength) // ok
                        continue;
                    int true_n_instants = Math.Min(data.file_n_instants, pathLength - data.chosen_instants_start); // ok

                    // ok if paths do not have id
                    GameObject go = new GameObject(keptPaths[i].ToString());
                    go.transform.parent = transform;
                    GlobalPath p = go.AddComponent<GlobalPath>();
                    p.atoms = new List<GlobalAtom>(true_n_instants);
                    // ok

                    Color32 color = UnityEngine.Random.ColorHSV(); // deal with colors

                    long nextPathPosition = br.BaseStream.Position + pathLength * n_of_bytes_per_atom;

                    br.BaseStream.Position += data.chosen_instants_start * n_of_bytes_per_atom;

                    for (int j = 0; j < true_n_instants; j += chosen_instant_step) {

                        Vector3 point = new Vector3();
                        if (data.useGPSCoords) {
                            Tools.SetGPSOrigin(Vector2dToVector2(data.GPSOrigin));

                            //point = Tools.GPSToXYZ(new Vector2(data.atomAttributes)).. * size coeff


                        }
                        else {
                            point.x = br.ReadSingle();// * sizeCoeff;
                            point.y = br.ReadSingle();// * sizeCoeff; //size coeff de l'attribut
                            point.z = br.ReadSingle();// * sizeCoeff;
                        }

                        point = Vector3.Max(point, lowerTruncature); // ok
                        point = Vector3.Min(point, upperTruncature); // ok 

                        p.atoms.Add(new GlobalAtom() {
                            time = (float)(j + data.chosen_instants_start), // how to deal with time and other attributes
                            point = point, 
                            path = p, // ok
                            indexInPath = j / chosen_instant_step,
                            BaseColor = color // deal with color
                        });

                        br.BaseStream.Position += (chosen_instant_step - 1) * n_of_bytes_per_atom;
                    }

                    br.BaseStream.Position = nextPathPosition;

                    paths.Add(p);
                    currentPath++;
                }

                Tools.EndClock("Loaded paths");
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
            // add attributes here : speded, acceleration .. 
        }


        protected override float InterpretTime(string word) {
            return 0;
        }
    }
}
