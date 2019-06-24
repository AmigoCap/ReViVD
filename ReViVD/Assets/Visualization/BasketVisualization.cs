using UnityEngine;
using System.Collections.Generic;

namespace Revivd {

    public class BasketVisualization : Visualization {
        public List<BasketPath> paths;
        public override IReadOnlyList<Path> PathsAsBase { get { return paths; } }

        public float scaleFactor = 5;

        public string filename;

        public void Reset() {
            districtSize = new Vector3(10, 10, 10);
        }

        protected override bool LoadFromFile() {
            paths = new List<BasketPath>();
            Dictionary<string, BasketPath> pathsDict = new Dictionary<string, BasketPath>();

            Dictionary<string, Color32> colorsDict = new Dictionary<string, Color32>();

            string[] rawData = System.IO.File.ReadAllLines(filename);

            for (int i = 0; i < rawData.Length; i++) {
                string[] words = CsvSplit(rawData[i], ',');    //Selon configuration de l'OS, mettre ',' ou '.'

                if (words.Length < 2)
                    continue;

                float x = scaleFactor * float.Parse(words[8]);
                float y = scaleFactor * float.Parse(words[10]);
                float z = scaleFactor * float.Parse(words[9]);
                if (badNumber(y) || badNumber(x) || badNumber(z))
                    continue;


                if (!pathsDict.TryGetValue(words[0], out BasketPath p) ) {
                    GameObject go = new GameObject(words[0]);
                    go.transform.parent = transform;
                    p = go.AddComponent<BasketPath>();
                    paths.Add(p);
                    pathsDict.Add(p.name, p);
                    
                    if (words[5] == "1") {
                        colorsDict.Add(p.name, Random.ColorHSV(0, 0.5f, 0.8f, 1));
                    }
                    if (words[5] == "0") {
                        colorsDict.Add(p.name, Random.ColorHSV(0.5f, 1, 0.4f, 0.8f));
                    }
                    //colorsDict.Add(p.name, Random.ColorHSV());
                    p.baseRadius = 0.1f;
                }

                BasketAtom a = new BasketAtom {
                    point = new Vector3(x, y, z),
                    path = p,
                    indexInPath = p.atoms.Count,
                    BaseColor = colorsDict[p.name]
                };

                p.atoms.Add(a);
            }

            return true;

        }

        protected override void Update() {
            base.Update();
        }

        public class BasketPath : Path {
            public List<BasketAtom> atoms = new List<BasketAtom>();
            public override IReadOnlyList<Atom> AtomsAsBase { get { return atoms; } }
        }

        public class BasketAtom : Atom {

        }
    }

}