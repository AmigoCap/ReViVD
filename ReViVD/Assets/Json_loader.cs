using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;

public class Json_loader : MonoBehaviour {

    public struct LoadingData {
        public struct Vector3D {
            public float x;
            public float y;
            public float z;
        }
        public struct Vector2D {
            public float x;
            public float y;
        }


        public struct AssetBundle {
            public string name;
            public string filename;
            public Vector3D position;
            public Vector3D rotation;
            public Vector3D scale;
        }

        public enum DataType {
            int32,
            float32,
            float64
        }

        public enum PathAttributeRole {
            n_atoms,
            id,
            other
        }

        public struct PathAttribute {
            public string name;
            public DataType type;
            public PathAttributeRole role;
            public string colorStart;
            public string colorEnd;
            public float valueColorStart;
            public float valueColorEnd;
        }

        public enum AtomAttributeRole {
            x,
            y,
            z,
            t,
            latitude,
            longitude,
            other
        }

        public struct AtomAttribute {
            public string name;
            public DataType type;
            public AtomAttributeRole role;
            public float sizeCoeff;
            public bool valueColorUseMinMax;
            public string colorStart;
            public string colorEnd;
            public float valueColorStart;
            public float valueColorEnd;
        }


        public string filename;
        public bool severalFiles;
        public int n_instants_per_file;
        public string endianness;
        public Vector3D districtSize;
        public Vector3D lowerTruncature;
        public Vector3D upperTruncature;
        public Vector2D gpsOrigin;

        public int file_n_paths;
        public bool randomPaths;
        public bool randomColorPaths;
        public int chosen_n_paths;
        public int chosen_paths_start;
        public int chosen_paths_end;
        public int chosen_paths_step;

        public bool constant_n_instants;
        public int file_n_instants;
        public int chosen_instants_start;
        public int chosen_instants_end;
        public int chosen_instants_step;

        public float spheresRadius;
        public float spheresAnimSpeed;
        public float spheresGlobalTime;
        public bool spheresDisplay;

        public AssetBundle[] assetBundles;
        public PathAttribute[] pathAttributes;
        public AtomAttribute[] atomAttributes;
    };

    void LoadJson() {
        StreamReader r = new StreamReader("C:\\Users\\ReViVD\\Documents\\Unity_projects\\ReViVD\\ReViVD\\json\\example.json");
        string json = r.ReadToEnd();
        LoadingData loadingdata = JsonConvert.DeserializeObject<LoadingData>(json);
        Debug.Log(loadingdata.atomAttributes[0].role);

    }

    private void Start() {
        LoadJson();
   
    }
}