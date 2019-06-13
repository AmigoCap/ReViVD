using UnityEngine;
using System.Collections.Generic;

namespace Revivd {

    public class FootVisualization : TimeVisualization {
        public List<FootPath> paths;
        public override IReadOnlyList<Path> PathsAsBase { get { return paths; } }
        public override IReadOnlyList<TimePath> PathsAsTime { get { return paths; } }

        public void Reset() {
            districtSize = new Vector3(10, 10, 10);
        }

        protected override bool LoadFromCSV() {
            if (dataFile == null)
                return false;
            paths = new List<FootPath>();
            Dictionary<string, FootPath> pathsDict = new Dictionary<string, FootPath>();

            Dictionary<string, Color32> colorsDict = new Dictionary<string, Color32>();

            string[] rawData = dataFile.text.Split(new char[] { '\n' });

            for (int i = 0; i < rawData.Length; i+=20) {
                string[] words = CsvSplit(rawData[i], ',');

                if (words.Length < 2)
                    continue;

                float t = InterpretTime(words[1]);
                float x = float.Parse(words[3].Replace('.', ','));
                float z = float.Parse(words[4].Replace('.', ','));
                if (badNumber(t) || badNumber(x) || badNumber(z))
                    continue;

                if (!pathsDict.TryGetValue(words[2], out FootPath p)) {
                    GameObject go = new GameObject(words[2]);
                    go.transform.parent = transform;
                    p = go.AddComponent<FootPath>();
                    paths.Add(p);
                    pathsDict.Add(p.name, p);
                    colorsDict.Add(p.name, Random.ColorHSV());
                    p.baseRadius = 0.2f;
                }

                FootAtom a = new FootAtom {
                    time = t,
                    point = new Vector3(x, t / 20, z),
                    path = p,
                    indexInPath = p.atoms.Count,
                    BaseColor = colorsDict[p.name]
                };

                p.atoms.Add(a);
            }

            return true;

        }

        protected override float InterpretTime(string word) {
            float time = float.Parse(word.Replace('.', ','));
            return time;
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
                    foreach (FootPath p in paths) {
                        p.RemoveTimeWindow();
                    }
                }
            }

            if (doTime) {
                foreach (FootPath p in paths) {
                    p.SetTimeWindow((Time.time - startTime) * 5 - 10, (Time.time - startTime) * 5 + 10);
                }
            }

            base.Update();
        }

        public class FootPath : TimePath {
            public List<FootAtom> atoms = new List<FootAtom>();
            public override IReadOnlyList<Atom> AtomsAsBase { get { return atoms; } }
            public override IReadOnlyList<TimeAtom> AtomsAsTime { get { return atoms; } }
        }

        public class FootAtom : TimeAtom {

        }
    }

}