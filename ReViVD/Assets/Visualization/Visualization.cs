using UnityEngine;
using System.Collections.Generic;
using System;

namespace Revivd {

    [DisallowMultipleComponent]
    public abstract class Visualization : MonoBehaviour {
        private static Visualization _instance;
        public static Visualization Instance { get { return _instance; } }

        public Vector3 districtSize;

        public TextAsset dataFile;

        public bool needsFullVerticesUpdate = false;

        public HashSet<Atom> selectedRibbons = new HashSet<Atom>();

        [NonSerialized]
        public Material material;

        public abstract IReadOnlyList<Path> PathsAsBase { get; }

        //DEBUG
        public bool debugMode = false;
        public int[] debugInts = { 0, 0, 0, 0 }; //Counters that reset every update, printed via getDebugData
        public bool getDebugData = false;
        private readonly List<int[]>[] districtsToHighlight = new List<int[]>[] { new List<int[]>(), new List<int[]>(), new List<int[]>() };

        private void OnDrawGizmos() {
            if (debugMode) {
                Gizmos.color = Color.green;
                Vector3 sides = new Vector3(districtSize.x * districts.GetLength(0), districtSize.y * districts.GetLength(1), districtSize.z * districts.GetLength(2));
                Gizmos.DrawWireCube(transform.TransformPoint(sides / 2 + lowerBoundary), sides);

                foreach (int[] d in districtsToHighlight[0]) {
                    Gizmos.DrawWireCube(transform.TransformPoint(districtSize / 2 + lowerBoundary + new Vector3(d[0] * districtSize.x, d[1] * districtSize.y, d[2] * districtSize.z)), districtSize);
                }

                Gizmos.color = Color.red;
                foreach (int[] d in districtsToHighlight[1]) {
                    Gizmos.DrawWireCube(transform.TransformPoint(districtSize / 2 + lowerBoundary + new Vector3(d[0] * districtSize.x, d[1] * districtSize.y, d[2] * districtSize.z)), districtSize);
                }

                Gizmos.color = Color.blue;
                foreach (int[] d in districtsToHighlight[2]) {
                    Gizmos.DrawWireCube(transform.TransformPoint(districtSize / 2 + lowerBoundary + new Vector3(d[0] * districtSize.x, d[1] * districtSize.y, d[2] * districtSize.z)), districtSize);
                }
            }
        }

        protected abstract bool LoadFromCSV();

        protected string[] CsvSplit(string row, char decimalChar = '.') { //En cas de CSV récalcitrant
            List<string> words = new List<string>();
            int i = 0;
            bool inQuotes = false;
            string currentWord = "";
            while (i < row.Length) {
                char c = row[i];
                if (c == ',') {
                    if (inQuotes)
                        currentWord += decimalChar;
                    else {
                        words.Add(currentWord);
                        currentWord = "";
                    }
                }
                else if (c == '.')
                    currentWord += decimalChar;
                else if (c == '"') {
                    if (inQuotes) {
                        if (row[i + 1] == '"') { //"" entre d'autres guillemets correspond au symbole "
                            currentWord += '"';
                            i++;
                        }
                        else {
                            inQuotes = false;
                        }
                    }
                    else
                        inQuotes = true;
                }
                else {
                    currentWord += c;
                }
                i++;
            }
            words.Add(currentWord);

            return words.ToArray();
        }

        int[] oldCameraDistrict;
        readonly bool[] thickTowardsPositive = { true, true, true }; //Définit la position du cube de 4x4 districts autour de la caméra

        protected virtual void Awake() {
            material = Resources.Load<Material>("Materials/Ribbon");

            if (_instance != null) {
                Debug.LogError("Multiple instances of visualization singleton");
                Destroy(Instance);
            }
            _instance = this;

            if (!LoadFromCSV())
                return;

            CreateDistricts();

            oldCameraDistrict = FindDistrict(transform.InverseTransformPoint(Camera.main.transform.position), false);
            needsFullVerticesUpdate = true;
        }

        protected void UpdateRendering() {
            int[] cameraDistrict = FindDistrict(transform.InverseTransformPoint(Camera.main.transform.position), false);
            if (cameraDistrict[0] != oldCameraDistrict[0] || cameraDistrict[1] != oldCameraDistrict[1] || cameraDistrict[2] != oldCameraDistrict[2]) {
                for (int i = 0; i < 3; i++) {
                    if (cameraDistrict[i] == oldCameraDistrict[i] + (thickTowardsPositive[i] ? 1 : -1)) {
                        thickTowardsPositive[i] = !thickTowardsPositive[i];
                    }
                    else if (cameraDistrict[i] != oldCameraDistrict[i]) {
                        needsFullVerticesUpdate = true;
                    }
                }
            }

            if (!needsFullVerticesUpdate) {
                for (int i = cameraDistrict[0] - (thickTowardsPositive[0] ? 1 : 2); i <= cameraDistrict[0] + (thickTowardsPositive[0] ? 2 : 1); i++) {
                    for (int j = cameraDistrict[1] - (thickTowardsPositive[1] ? 1 : 2); j <= cameraDistrict[1] + (thickTowardsPositive[1] ? 2 : 1); j++) {
                        for (int k = cameraDistrict[2] - (thickTowardsPositive[2] ? 1 : 2); k <= cameraDistrict[2] + (thickTowardsPositive[2] ? 2 : 1); k++) {
                            try {
                                foreach (Atom a in districts[i, j, k].atoms_line) {
                                    a.ShouldUpdateVertices = true;
                                }
                            }
                            catch (System.IndexOutOfRangeException) {
                                continue;
                            }
                        }
                    }
                }
            }
            else {
                needsFullVerticesUpdate = false;
                foreach (Path p in PathsAsBase) {
                    p.needsVerticesUpdate = true;
                    p.forceFullVerticesUpdate = true;
                }
            }

            foreach (Path p in PathsAsBase) {
                p.UpdatePath();
            }

            oldCameraDistrict = cameraDistrict;

            if (getDebugData) { //DEBUG
                getDebugData = false;
                string s = "Debug ints: ";
                foreach (int i in debugInts)
                    s += i.ToString() + '\t';
                Debug.Log(s);
            }

            if (debugMode) {
                for (int i = 0; i < 4; i++)
                    debugInts[i] = 0;
            }
        }

        public int GetPathIndex(string name) {
            int c = PathsAsBase.Count;
            for (int i = 0; i < c; i++) {
                if (PathsAsBase[i].name == name)
                    return i;
            }
            return -1;
        }

        public struct District { //Subdivision discrète de la visualisation dans l'espace pour optimisation
            public Atom[] atoms_line; //Tous les atomes dont le ruban, étendu en ligne inifinie, traverse le district
            public Atom[] atoms_segment; //Tous les atomes dont le ruban fini traverse le district
            public Vector3 center;
        }

        private Vector3 lowerBoundary; //Coordonnées de l'atome de coordonnées minimales
        private Vector3 upperBoundary; //Coordonnées de l'atome de coordonnées maximales

        public District[,,] districts; //Array tridimensionnel de Districts

        private void CalculateBoundaries() {
            lowerBoundary = transform.InverseTransformPoint(PathsAsBase[0].transform.TransformPoint(PathsAsBase[0].AtomsAsBase[0].point));
            upperBoundary = transform.InverseTransformPoint(PathsAsBase[0].transform.TransformPoint(PathsAsBase[0].AtomsAsBase[0].point));
            foreach (Path p in PathsAsBase) {
                foreach (Atom a in p.AtomsAsBase) {
                    lowerBoundary = Vector3.Min(lowerBoundary, transform.InverseTransformPoint(p.transform.TransformPoint(a.point)));
                    upperBoundary = Vector3.Max(upperBoundary, transform.InverseTransformPoint(p.transform.TransformPoint(a.point)));
                }
            }
        }

        //Donne le district dans lequel se trouve un point dans les coordonnées de la visualisation.
        //L'option "crop" renvoie toujours un district réel (si le point est à l'extérieur de la visualisation, on prend le district le plus proche)
        public int[] FindDistrict(Vector3 point, bool crop = false) {
            int[] district;
            if (crop) {
                //On utilise Min et Max pour éviter les soucis avec les flottants
                district = new int[] {
                Math.Min(districts.GetLength(0) - 1, Math.Max(0, Mathf.FloorToInt((point.x - lowerBoundary.x) / districtSize.x))),
                Math.Min(districts.GetLength(1) - 1, Math.Max(0, Mathf.FloorToInt((point.y - lowerBoundary.y) / districtSize.y))),
                Math.Min(districts.GetLength(2) - 1, Math.Max(0, Mathf.FloorToInt((point.z - lowerBoundary.z) / districtSize.z)))
            };
            }
            else {
                district = new int[] {
                Mathf.FloorToInt((point.x - lowerBoundary.x) / districtSize.x),
                Mathf.FloorToInt((point.y - lowerBoundary.y) / districtSize.y),
                Mathf.FloorToInt((point.z - lowerBoundary.z) / districtSize.z)
            };
            }
            return district;
        }

        private void CreateDistricts() { //Crée et remplit districts
            CalculateBoundaries();
            int[] numberOfDistricts = {
            Mathf.FloorToInt((upperBoundary.x - lowerBoundary.x) / districtSize.x + 1),
            Mathf.FloorToInt((upperBoundary.y - lowerBoundary.y) / districtSize.y + 1),
            Mathf.FloorToInt((upperBoundary.z - lowerBoundary.z) / districtSize.z + 1) };
            districts = new District[numberOfDistricts[0], numberOfDistricts[1], numberOfDistricts[2]];

            HashSet<Atom>[,,] atomRepartition_line = new HashSet<Atom>[numberOfDistricts[0], numberOfDistricts[1], numberOfDistricts[2]];
            HashSet<Atom>[,,] atomRepartition_segment = new HashSet<Atom>[numberOfDistricts[0], numberOfDistricts[1], numberOfDistricts[2]];
            for (int i = 0; i < numberOfDistricts[0]; i++) {
                for (int j = 0; j < numberOfDistricts[1]; j++) {
                    for (int k = 0; k < numberOfDistricts[2]; k++) {
                        atomRepartition_line[i, j, k] = new HashSet<Atom>();
                        atomRepartition_segment[i, j, k] = new HashSet<Atom>();
                    }
                }
            }

            foreach (Path p in PathsAsBase) {
                Vector3 point = transform.InverseTransformPoint(p.transform.TransformPoint(p.AtomsAsBase[0].point));

                for (int i = 0; i < p.AtomsAsBase.Count - 1; i++) {
                    Vector3 nextPoint = transform.InverseTransformPoint(p.transform.TransformPoint(p.AtomsAsBase[i + 1].point));
                    Vector3 delta = (nextPoint - point).normalized;

                    //On détermine les intersections avec les bords de la visualisation
                    Vector3 retroIntersect, proIntersect;
                    Vector3 retroCoeffs = Vector3.negativeInfinity;
                    Vector3 proCoeffs = Vector3.positiveInfinity;

                    float temp;

                    temp = (upperBoundary.x - point.x) / delta.x;
                    if (temp > 0) proCoeffs.x = temp; else retroCoeffs.x = temp;
                    temp = (upperBoundary.y - point.y) / delta.y;
                    if (temp > 0) proCoeffs.y = temp; else retroCoeffs.y = temp;
                    temp = (upperBoundary.z - point.z) / delta.z;
                    if (temp > 0) proCoeffs.z = temp; else retroCoeffs.z = temp;

                    temp = (lowerBoundary.x - point.x) / delta.x;
                    if (temp > 0) proCoeffs.x = temp; else retroCoeffs.x = temp;
                    temp = (lowerBoundary.y - point.y) / delta.y;
                    if (temp > 0) proCoeffs.y = temp; else retroCoeffs.y = temp;
                    temp = (lowerBoundary.z - point.z) / delta.z;
                    if (temp > 0) proCoeffs.z = temp; else retroCoeffs.z = temp;

                    proIntersect = Mathf.Min(proCoeffs.x, proCoeffs.y, proCoeffs.z) * delta + point;
                    retroIntersect = Mathf.Max(retroCoeffs.x, retroCoeffs.y, retroCoeffs.z) * delta + point;

                    //On traduit ces intersections en districts
                    int[] retroDistrict = FindDistrict(retroIntersect, true);
                    int[] proDistrict = FindDistrict(proIntersect, true);

                    //On obtient aussi les districts des points
                    int[] pointDistrict = FindDistrict(point, true);
                    int[] nextPointDistrict = FindDistrict(nextPoint, true);

                    //Algorithme de Bresenham en 3D : on détermine tous les districts entre ces deux intersections
                    List<int[]> districts_line = Tools.Bresenham(retroDistrict, proDistrict);
                    foreach (int[] d in districts_line) {
                        atomRepartition_line[d[0], d[1], d[2]].Add(p.AtomsAsBase[i]);
                    }
                    List<int[]> districts_segment = Tools.Bresenham(pointDistrict, nextPointDistrict);
                    foreach (int[] d in districts_segment) {
                        atomRepartition_segment[d[0], d[1], d[2]].Add(p.AtomsAsBase[i]);
                    }

                    point = nextPoint;
                }
            }

            for (int x = 0; x < numberOfDistricts[0]; x++) {
                for (int y = 0; y < numberOfDistricts[1]; y++) {
                    for (int z = 0; z < numberOfDistricts[2]; z++) {
                        districts[x, y, z] = new District() {
                            center = new Vector3(districtSize.x * x, districtSize.y * y, districtSize.z * z) + districtSize / 2 + lowerBoundary
                        };
                        districts[x, y, z].atoms_line = new Atom[atomRepartition_line[x, y, z].Count];
                        atomRepartition_line[x, y, z].CopyTo(districts[x, y, z].atoms_line);
                        districts[x, y, z].atoms_segment = new Atom[atomRepartition_segment[x, y, z].Count];
                        atomRepartition_segment[x, y, z].CopyTo(districts[x, y, z].atoms_segment);
                    }
                }
            }
        }
    }
}