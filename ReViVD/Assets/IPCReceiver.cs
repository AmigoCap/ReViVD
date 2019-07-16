using UnityEngine;
using System.Threading.Tasks;
using System;
using Newtonsoft.Json;

namespace Revivd {
    public class IPCReceiver : MonoBehaviour {
        private static IPCReceiver _instance;
        public static IPCReceiver Instance { get { return _instance; } }

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
        public LoadingData data;

        public void CatchData() {
            try {
                data = JsonConvert.DeserializeObject<IPCReceiver.LoadingData>(Console.ReadLine());
            }
            catch (System.Exception e) {
                Debug.Log("Error deserializing data: " + e.Message);
            }

            Debug.Log(data.file_n_paths);
        }

        Task receiverTask;
        bool isReceiving = false;
        public void StartLiveReceiving() {
            if (isReceiving)
                return;
            receiverTask = new Task(ReceiveLiveCommands);
            receiverTask.Start();
            isReceiving = true;
        }

        enum Command {
            DisplaySpheres = 0,     //bool
            AnimSpheres,            //bool
            DropSpheres,            //void
            SetSpheresGlobalTime,   //float
            UseGlobalTime,          //void
            SetSpheresAnimSpeed,    //float
            SetSpheresRadius,       //float
            Stop                    //void
        }

        void ReceiveLiveCommands() {
            Command currentCommand = (Command)Console.Read();
            GlobalVisualization viz = (GlobalVisualization)Visualization.Instance;
            while (currentCommand != Command.Stop) {
                switch (currentCommand) {
                    case Command.DisplaySpheres:
                        viz.displayTimeSpheres = bool.Parse(Console.ReadLine());
                        break;
                    case Command.AnimSpheres:
                        viz.doTimeSphereAnimation = bool.Parse(Console.ReadLine());
                        break;
                    case Command.DropSpheres:
                        viz.useGlobalTime = false;
                        viz.DropSpheres();
                        break;
                    case Command.SetSpheresGlobalTime:
                        viz.globalTime = float.Parse(Console.ReadLine());
                        break;
                    case Command.UseGlobalTime:
                        viz.useGlobalTime = true;
                        break;
                    case Command.SetSpheresAnimSpeed:
                        viz.timeSphereAnimationSpeed = float.Parse(Console.ReadLine());
                        break;
                    case Command.SetSpheresRadius:
                        viz.timeSphereRadius = float.Parse(Console.ReadLine());
                        break;
                    default:
                        break;
                }

                currentCommand = (Command)Console.Read();
            }
        }

        private void Awake() {
            if (_instance != null) {
                Debug.LogWarning("Multiple instances of IPCReceiver singleton");
            }
            _instance = this;
        }
    }
}
