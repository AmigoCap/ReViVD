using UnityEngine;
using System.Collections.Generic;

namespace Revivd {

    public class LouRugbyVisualization : TimeVisualization {
        public List<LouRugbyPath> paths;
        public override IReadOnlyList<Path> PathsAsBase { get { return paths; } }
        public override IReadOnlyList<TimePath> PathsAsTime { get { return paths; } }

        public void Reset() {
            districtSize = new Vector3(10, 10, 10);
        }

        protected override bool LoadFromCSV() {
            if (dataFile == null)
                return false;
            paths = new List<LouRugbyPath>();
            Dictionary<string, LouRugbyPath> pathsDict = new Dictionary<string, LouRugbyPath>();

            Dictionary<string, Color32> colorsDict = new Dictionary<string, Color32>();

            string[] rawData = dataFile.text.Split(new char[] { '\n' });

            for (int i = 0; i < rawData.Length / 20; i++) {
                string[] words = CsvSplit(rawData[20 * i], ',');    //Selon configuration de l'OS, mettre ',' ou '.'

                if (words.Length < 2)
                    continue;

                if (!pathsDict.TryGetValue(words[0], out LouRugbyPath p)) {
                    GameObject go = new GameObject(words[0]);
                    go.transform.parent = transform;
                    p = go.AddComponent<LouRugbyPath>();
                    paths.Add(p);
                    pathsDict.Add(p.name, p);
                    colorsDict.Add(p.name, Random.ColorHSV());
                }

                float t = InterpretTime(words[4]);
                LouRugbyAtom a = new LouRugbyAtom {
                    time = t,
                    point = new Vector3(float.Parse(words[1]), t / 20, float.Parse(words[2])),
                    path = p,
                    indexInPath = p.atoms.Count
                };

                a.BaseColor = colorsDict[p.name];
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

        private void Update() {
            if (Input.GetMouseButtonDown(0)) {
                startTime = Time.time;
            }
            if (Input.GetMouseButtonDown(1)) {
                doTime = !doTime;
                if (doTime) {
                    startTime = Time.time;
                }
                else {
                    foreach (LouRugbyPath p in paths) {
                        p.RemoveTimeWindow();
                    }
                }
            }

            if (doTime) {
                foreach (LouRugbyPath p in paths) {
                    p.SetTimeWindow((Time.time - startTime) * 5 - 10, (Time.time - startTime) * 5 + 10);
                }
            }

            UpdateRendering();
        }

        public class LouRugbyPath : TimePath {
            public List<LouRugbyAtom> atoms = new List<LouRugbyAtom>();
            public override IReadOnlyList<Atom> AtomsAsBase { get { return atoms; } }
            public override IReadOnlyList<TimeAtom> AtomsAsTime { get { return atoms; } }
        }

        public class LouRugbyAtom : TimeAtom {

        }
    }

}