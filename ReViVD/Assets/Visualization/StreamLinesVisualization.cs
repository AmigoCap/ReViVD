using UnityEngine;
using System.Collections.Generic;

namespace Revivd {

    public class StreamLinesVisualization : TimeVisualization {
        public List<StreamLinesPath> paths;
        public override IReadOnlyList<Path> PathsAsBase { get { return paths; } }
        public override IReadOnlyList<TimePath> PathsAsTime { get { return paths; } }

        public string filename;
        public enum enumColorAttribute { random, mach };
        [SerializeField]
        private enumColorAttribute colorAttribute = enumColorAttribute.mach;
        private enumColorAttribute _colorAttribute;
        public enumColorAttribute ColorAttribute {
            get => colorAttribute;
            set {
                if (_colorAttribute == value)
                    return;
                _colorAttribute = value;
                colorAttribute = _colorAttribute;
            }
        }


        public void Reset() {
            districtSize = new Vector3(10, 10, 10);
        }


        public Vector3 maxPoint = new Vector3(500, 500, 500);
        public Vector3 minPoint = new Vector3(-500, -500, -500);


        protected override bool LoadFromFile() {
            paths = new List<StreamLinesPath>();
            Dictionary<string, StreamLinesPath> pathsDict = new Dictionary<string, StreamLinesPath>();
            Dictionary<string, Color32> colorsDict = new Dictionary<string, Color32>();

            string[] rawData = System.IO.File.ReadAllLines(filename);

            for (int i = 0; i < rawData.Length; i += 1) {
                string[] words = CsvSplit(rawData[i], ',');

                if (words.Length < 2)
                    continue;

                float mach = Tools.InterpretExponent(words[0]);
                float t = InterpretTime(words[1]);
                float x = Tools.InterpretExponent(words[2]);
                float y = Tools.InterpretExponent(words[3]);
                float z = Tools.InterpretExponent(words[4]);
                if (badNumber(t) || badNumber(x) || badNumber(y) || badNumber(z))
                    continue;

                if (!pathsDict.TryGetValue(words[5], out StreamLinesPath p)) {
                    GameObject go = new GameObject(words[5]);
                    go.transform.parent = transform;
                    p = go.AddComponent<StreamLinesPath>();
                    paths.Add(p);
                    pathsDict.Add(p.name, p);
                    colorsDict.Add(p.name, Random.ColorHSV());
                    p.baseRadius = 0.2f;
                }

                Vector3 point =  new Vector3(1000 * y, 1000 * x, 1000 * z);
                point = Vector3.Max(point, minPoint);
                point = Vector3.Min(point, maxPoint);

                StreamLinesAtom a = new StreamLinesAtom {
                    time = 1000 * t,
                    point = point,
                    path = p,
                    mach = mach,
                    indexInPath = p.atoms.Count,
                };

                if (ColorAttribute == enumColorAttribute.mach)
                    a.BaseColor = Color.Lerp(Color.blue, Color.red, a.mach / 1.1f);
                else
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
                    foreach (StreamLinesPath p in paths) {
                        p.RemoveTimeWindow();
                    }
                }
            }

            if (doTime) {
                foreach (StreamLinesPath p in paths) {
                    p.SetTimeWindow((Time.time - startTime) * 5 - 10, (Time.time - startTime) * 5 + 10);
                }
            }

            base.Update();
        }

        public class StreamLinesPath : TimePath {
            public List<StreamLinesAtom> atoms = new List<StreamLinesAtom>();
            public override IReadOnlyList<Atom> AtomsAsBase { get { return atoms; } }
            public override IReadOnlyList<TimeAtom> AtomsAsTime { get { return atoms; } }
        }

        public class StreamLinesAtom : TimeAtom {
            public float mach;

        }
    }

}