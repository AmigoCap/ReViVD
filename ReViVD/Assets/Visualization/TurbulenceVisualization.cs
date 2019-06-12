using UnityEngine;
using System.Collections.Generic;

namespace Revivd {

    public class TurbulenceVisualization : Visualization {
        public List<TurbulencePath> paths;
        public override IReadOnlyList<Path> PathsAsBase { get { return paths; } }
        public IReadOnlyList<TurbulencePath> PathsAsTurbulence { get { return paths; } }

        public float scaleCoeff = 5000;

        public void Reset() {
            districtSize = new Vector3(10, 10, 10);
        }

        protected override bool LoadFromCSV() {
            if (dataFile == null)
                return false;
            paths = new List<TurbulencePath>();
            Dictionary<string, TurbulencePath> pathsDict = new Dictionary<string, TurbulencePath>();

            Dictionary<string, Color32> colorsDict = new Dictionary<string, Color32>();

            string[] rawData = dataFile.text.Split(new char[] { '\n' });

            for (int i = 0; i < rawData.Length; i++) {
                string[] words = CsvSplit(rawData[i], ',');    //Selon configuration de l'OS, mettre ',' ou '.'

                if (words.Length < 2)
                    continue;

                float press = Tools.InterpretExponent(words[0]);
                float dens = Tools.InterpretExponent(words[1]);
                float rot = Tools.InterpretExponent(words[2]);
                float x = Tools.InterpretExponent(words[3]);
                float y = Tools.InterpretExponent(words[5]);
                float z = Tools.InterpretExponent(words[4]);

                if (badNumber(x) || badNumber(y) || badNumber(z))
                    continue;

                if (!pathsDict.TryGetValue(words[7], out TurbulencePath p)) {
                    GameObject go = new GameObject(words[7]);
                    go.transform.parent = transform;
                    p = go.AddComponent<TurbulencePath>();
                    paths.Add(p);
                    pathsDict.Add(p.name, p);
                    colorsDict.Add(p.name, Random.ColorHSV());
                    p.baseRadius = 0.2f;
                }
                
                TurbulenceAtom a = new TurbulenceAtom {
                    pressureStagnation = press,
                    density = dens,
                    rotatingMach = rot,
                    point = scaleCoeff * new Vector3(x, y, z),
                    path = p,
                    indexInPath = p.atoms.Count,
                    //BaseColor = Color32.Lerp(new Color32(255, 0, 0, 255), new Color32(0, 0, 255, 255), press / 185185f)
                    //BaseColor = Color32.Lerp(new Color32(255, 0, 0, 255), new Color32(0, 0, 255, 255), dens)
                    BaseColor = Color32.Lerp(new Color32(255, 0, 0, 255), new Color32(0, 0, 255, 255), rot)

                };

                p.atoms.Add(a);
            }
            return true;
        }

        private void Update() {
            UpdateRendering();
        }

        public class TurbulencePath : Path {
            public List<TurbulenceAtom> atoms = new List<TurbulenceAtom>();
            public override IReadOnlyList<Atom> AtomsAsBase { get { return atoms; } }
            public IReadOnlyList<TurbulenceAtom> AtomsAsTurbulence { get { return atoms; } }
        }

        public class TurbulenceAtom : Atom {
            public float pressureStagnation;
            public float density;
            public float rotatingMach;

        }
    }

}
