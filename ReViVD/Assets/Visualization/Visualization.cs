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

        public Material material;

        public abstract IReadOnlyList<Path> PathsAsBase { get; }

        //DEBUG
        public bool debugMode = false;
        public int[] debugInts = { 0, 0, 0, 0 }; //Counters that reset every update, printed via getDebugData
        public bool getDebugData = false;
        public readonly HashSet<int[]>[] districtsToHighlight = { new HashSet<int[]>(new CoordsEqualityComparer()), new HashSet<int[]>(new CoordsEqualityComparer()), new HashSet<int[]>(new CoordsEqualityComparer()) };

        private void OnDrawGizmos() {
            if (debugMode) {
                Gizmos.color = Color.green;
                foreach (int[] d in districtsToHighlight[0]) {
                    Gizmos.DrawWireCube(transform.TransformPoint(districtSize / 2 + new Vector3(d[0] * districtSize.x, d[1] * districtSize.y, d[2] * districtSize.z)), districtSize);
                }

                Gizmos.color = Color.red;
                foreach (int[] d in districtsToHighlight[1]) {
                    Gizmos.DrawWireCube(transform.TransformPoint(districtSize / 2 + new Vector3(d[0] * districtSize.x, d[1] * districtSize.y, d[2] * districtSize.z)), districtSize);
                }

                Gizmos.color = Color.blue;
                foreach (int[] d in districtsToHighlight[2]) {
                    Gizmos.DrawWireCube(transform.TransformPoint(districtSize / 2 + new Vector3(d[0] * districtSize.x, d[1] * districtSize.y, d[2] * districtSize.z)), districtSize);
                }
            }
        }

        protected abstract bool LoadFromCSV();

        protected bool badNumber(float f) {
            return float.IsNaN(f) || float.IsNegativeInfinity(f) || float.IsPositiveInfinity(f);
        }

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

        protected virtual void Awake() {
            if (material == null)
                material = Resources.Load<Material>("Materials/Ribbon");

            if (_instance != null) {
                Debug.LogWarning("Multiple instances of visualization singleton");
            }
            _instance = this;

            if (!LoadFromCSV())
                return;

            CreateDistricts();
        }

        protected void UpdateRendering() {
            foreach (Path p in PathsAsBase) {
                p.UpdatePath();
            }

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
            public List<Atom> atoms_segment; //Tous les atomes dont le ruban fini traverse le district
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

        private void CreateDistricts() { //Crée et remplit districts
            districts = new Dictionary<int[], District>(new CoordsEqualityComparer());

            foreach (Path p in PathsAsBase) {
                //Rappel : les coordonnées issues du csv (donc celles de Atom.point) sont les coordonnées relatives au Path dans le repère du Path.
                Vector3 point = transform.InverseTransformPoint(p.transform.TransformPoint(p.AtomsAsBase[0].point));

                for (int i = 0; i < p.AtomsAsBase.Count - 1; i++) {
                    Vector3 nextPoint = transform.InverseTransformPoint(p.transform.TransformPoint(p.AtomsAsBase[i + 1].point));
                    Vector3 delta = (nextPoint - point).normalized;


                    //Algorithme d'Amanatides en 3D : on détermine tous les districts entre ces deux districts
                    List<int[]> districts_segment = Tools.Amanatides(point, nextPoint);
                    foreach (int[] c in districts_segment) {
                        if (!districts.TryGetValue(c, out District d)) {
                            d = new District() {
                                atoms_segment = new List<Atom>()
                            };
                            districts.Add(c, d);
                            districtsToHighlight[0].Add(c);
                        }
                        d.atoms_segment.Add(p.AtomsAsBase[i]);
                    }

                    point = nextPoint;
                }
            }
        }
    }
}