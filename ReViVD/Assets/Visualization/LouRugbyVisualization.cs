using UnityEngine;
using System.Collections.Generic;
using System.Globalization;

namespace Revivd {

    public class LouRugbyVisualization : TimeVisualization {
        public List<LouRugbyPath> Paths { get; set; }
        public override IReadOnlyList<Path> PathsAsBase { get { return Paths; } }
        public override IReadOnlyList<TimePath> PathsAsTime { get { return Paths; } }

        public void Reset() {
            districtSize = new Vector3(10, 10, 10);
        }

        protected override bool LoadFromCSV(string filename) {
            TextAsset file = Resources.Load<TextAsset>(filename);
            if (file == null)
                return false;
            Paths = new List<LouRugbyPath>();
            Dictionary<string, LouRugbyPath> PathsDict = new Dictionary<string, LouRugbyPath>();

            Dictionary<string, Color32> colorsDict = new Dictionary<string, Color32>();

            string[] rawData = file.text.Split(new char[] { '\n' });

            for (int i = 0; i < rawData.Length / 50; i++) {
                string[] words = CsvSplit(rawData[50 * i], ',');    //Selon configuration de l'OS, mettre ',' ou '.'

                if (words.Length < 2)
                    continue;

                LouRugbyPath p;
                if (!PathsDict.TryGetValue(words[0], out p)) {
                    p = new LouRugbyPath() { ID = words[0], baseRadius = 0.1f };
                    Paths.Add(p);
                    PathsDict.Add(p.ID, p);
                    colorsDict.Add(p.ID, Random.ColorHSV());
                }

                float t = InterpretTime(words[4]);
                LouRugbyAtom a = new LouRugbyAtom {
                    time = t,
                    point = new Vector3(float.Parse(words[1]), t/20, float.Parse(words[2])),
                    path = p
                };

                a.baseColor = colorsDict[p.ID];
                p.atoms.Add(a);
            }

            return true;

        }

        protected override float InterpretTime(string word) {
            float time = float.Parse(word.Replace('.', ','));
            return time;
        }

        bool doTime = false;

        private void Start() {
            startTime = Time.time;
        }

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
                    foreach (LouRugbyPath p in Paths) {
                        p.RemoveTimeWindow();
                    }
                }
            }

            if (doTime) {
                foreach (LouRugbyPath p in Paths) {
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