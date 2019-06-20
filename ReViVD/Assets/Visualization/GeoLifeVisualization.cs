using UnityEngine;
using System.Collections.Generic;

namespace Revivd {

    public class GeoLifeVisualization : TimeVisualization {
        public List<GeoLifePath> paths;
        public override IReadOnlyList<Path> PathsAsBase { get { return paths; } }
        public override IReadOnlyList<TimePath> PathsAsTime { get { return paths; } }

        public void Reset() {
            districtSize = new Vector3(10, 10, 10);
        }

        public float maxAltitude = 2000;
        public float minAltitude = -777;


        protected override bool LoadFromFile() {
            if (dataFile == null)
                return false;
            paths = new List<GeoLifePath>();
            Dictionary<string, GeoLifePath> pathsDict = new Dictionary<string, GeoLifePath>();

            Dictionary<string, Color32> colorsDict = new Dictionary<string, Color32>();

            string[] rawData = dataFile.text.Split(new char[] { '\n' });

            for (int i = 0; i < rawData.Length; i+=1) {
                string[] words = CsvSplit(rawData[i], ',');

                if (words.Length < 2)
                    continue;

                float x = float.Parse(words[1].Replace('.', ','));
                float z = float.Parse(words[2].Replace('.', ','));
                float y = float.Parse(words[4].Replace('.', ','));


                float t = InterpretTime(words[5]);

                if (badNumber(t) || badNumber(x) || badNumber(y) || badNumber(z) || y ==-777)
                    continue;

                if (!pathsDict.TryGetValue(words[0], out GeoLifePath p)) {
                    GameObject go = new GameObject(words[0]);
                    go.transform.parent = transform;
                    p = go.AddComponent<GeoLifePath>();
                    paths.Add(p);
                    pathsDict.Add(p.name, p);
                    colorsDict.Add(p.name, Random.ColorHSV());
                    p.baseRadius = 0.2f;
                }

                y = Mathf.Max(y, minAltitude);
                y = Mathf.Min(y, maxAltitude);
       
                GPSEncoder.SetLocalOrigin(new Vector2(39.919223f, 116.374909f));
                Vector3 point = GPSEncoder.GPSToUCS(x, z);
                point.x /= 100;
                point.z /= 100;
                point.y = y/100;
                Debug.Log(point);

                GeoLifeAtom a = new GeoLifeAtom {
                    time = t,
                    point = point,
                    path = p,
                    indexInPath = p.atoms.Count,
                    BaseColor = colorsDict[p.name]
                };

                p.atoms.Add(a);
            }

            return true;

        }

        protected override float InterpretTime(string word) {
            float time = float.Parse(word.Replace('.', ',')) - 39173f ;
            return time;
        }

        private float InterpretGPSCoordinates(string word) {
            string[] coordinates = word.Split('.');
            float coord = float.Parse(coordinates[0]) * 1000 + float.Parse(coordinates[1]);
            return coord;
        }

        bool doTime = false;

        private float startTime = 0;

        protected override void Update() {
            if (Input.GetMouseButtonDown(0)) {
                startTime = Time.time;
            }
            if (Input.GetMouseButtonDown(1)) {
                doTime = !doTime;
                if (doTime) {
                    startTime = Time.time;
                }
                else {
                    foreach (GeoLifePath p in paths) {
                        p.RemoveTimeWindow();
                    }
                }
            }

            if (doTime) {
                foreach (GeoLifePath p in paths) {
                    p.SetTimeWindow((Time.time - startTime) * 5 - 10, (Time.time - startTime) * 5 + 10);
                }
            }

            base.Update();
        }

        public class GeoLifePath : TimePath {
            public List<GeoLifeAtom> atoms = new List<GeoLifeAtom>();
            public override IReadOnlyList<Atom> AtomsAsBase { get { return atoms; } }
            public override IReadOnlyList<TimeAtom> AtomsAsTime { get { return atoms; } }
        }

        public class GeoLifeAtom : TimeAtom {

        }
    }

}