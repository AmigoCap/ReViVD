using UnityEngine;
using System.Collections.Generic;

namespace Revivd {

    public class LouRugbyVisualization : TimeVisualization {
        public List<LouRugbyPath> paths;
        public override IReadOnlyList<Path> PathsAsBase { get { return paths; } }
        public override IReadOnlyList<TimePath> PathsAsTime { get { return paths; } }

        public string filename;

        public void Reset() {
            districtSize = new Vector3(10, 10, 10);
        }

        protected override bool LoadFromFile() {
            paths = new List<LouRugbyPath>();
            Dictionary<string, LouRugbyPath> pathsDict = new Dictionary<string, LouRugbyPath>();

            Dictionary<string, Color32> colorsDict = new Dictionary<string, Color32>();

            string[] rawData = System.IO.File.ReadAllLines(filename);

            for (int i = 0; i < rawData.Length; i+=10) {
                string[] words = CsvSplit(rawData[i], ',');    //Selon configuration de l'OS, mettre ',' ou '.'

                if (words.Length < 2)
                    continue;

                float t = InterpretTime(words[3]);
                float x = float.Parse(words[1]);
                float z = float.Parse(words[2]);
                if (badNumber(t) || badNumber(x) || badNumber(z))
                    continue;

                if (!pathsDict.TryGetValue(words[0], out LouRugbyPath p)) {
                    GameObject go = new GameObject(words[0]);
                    go.transform.parent = transform;
                    p = go.AddComponent<LouRugbyPath>();
                    paths.Add(p);
                    pathsDict.Add(p.name, p);
                    colorsDict.Add(p.name, Random.ColorHSV());
                    p.baseRadius = 0.2f;
                }
                
                LouRugbyAtom a = new LouRugbyAtom {
                    time = t,
                    point = new Vector3(x, 0, z),
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
        /*
        private float InterpretGPSCoordinates(string word) {
            string[] coordinates = word.Split('.');
            float coord = float.Parse(coordinates[0]) * 1000 + float.Parse(coordinates[1]);
            return coord;
        }*/

        public class LouRugbyPath : TimePath {
            public List<LouRugbyAtom> atoms = new List<LouRugbyAtom>();
            public override IReadOnlyList<Atom> AtomsAsBase { get { return atoms; } }
            public override IReadOnlyList<TimeAtom> AtomsAsTime { get { return atoms; } }
        }

        public class LouRugbyAtom : TimeAtom {

        }
    }

}