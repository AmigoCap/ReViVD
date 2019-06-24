using UnityEngine;
using System.Collections.Generic;

namespace Revivd {

    public class AirTrafficVisualization : TimeVisualization {
        public List<AirTrafficPath> paths;
        public override IReadOnlyList<Path> PathsAsBase { get { return paths; } }
        public override IReadOnlyList<TimePath> PathsAsTime { get { return paths; } }

        public string filename;

        public void Reset() {
            districtSize = new Vector3(15, 15, 15);
        }

        protected override bool LoadFromFile() {
            paths = new List<AirTrafficPath>();
            Dictionary<string, AirTrafficPath> pathsDict = new Dictionary<string, AirTrafficPath>();

            string[] rawData = System.IO.File.ReadAllLines(filename);

            foreach (string row in rawData) {
                string[] words = CsvSplit(row, ',');    //Selon configuration de l'OS, mettre ',' ou '.'

                if (words.Length < 2)
                    continue;

                float t = InterpretTime(words[1]);
                float x = float.Parse(words[2]);
                float y = float.Parse(words[4]);
                float z = float.Parse(words[3]);
                if (badNumber(t) || badNumber(x) || badNumber(y) || badNumber(z))
                    continue;

                if (!pathsDict.TryGetValue(words[0], out AirTrafficPath p)) {
                    GameObject go = new GameObject(words[0]);
                    go.transform.parent = transform;
                    p = go.AddComponent<AirTrafficPath>();
                    paths.Add(p);
                    pathsDict.Add(p.name, p);
                }

                AirTrafficAtom a = new AirTrafficAtom {
                    time = t,
                    point = new Vector3(x, y, z),
                    path = p,
                    indexInPath = p.atoms.Count
                };

                a.BaseColor = Color32.Lerp(new Color32(255, 0, 0, 255), new Color32(0, 0, 255, 255), a.point.y / 400f);
                p.atoms.Add(a);
            }

            AirTrafficPath p60 = pathsDict["60"];
            int c = p60.AtomsAsBase.Count;
            for (int i = 0; i < c; i += 2)
                p60.specialRadii.Add(i, 0.3f);

            return true;
        }

        protected override float InterpretTime(string word) {
            float time = 0;
            time += float.Parse(word.Substring(6, 6).Replace('.', ','));
            time += float.Parse(word.Substring(3, 2)) * 60;
            time += float.Parse(word.Substring(0, 2)) * 3600;
            return time;
        }

        public class AirTrafficPath : TimePath {
            public List<AirTrafficAtom> atoms = new List<AirTrafficAtom>();
            public override IReadOnlyList<Atom> AtomsAsBase { get { return atoms; } }
            public override IReadOnlyList<TimeAtom> AtomsAsTime { get { return atoms; } }
        }

        public class AirTrafficAtom : TimeAtom {
        }
    }

}