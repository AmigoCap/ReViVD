using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace Revivd {

    public class Lyon6Visualization : TimeVisualization {
        public List<Lyon6Path> paths;
        public override IReadOnlyList<Path> PathsAsBase { get { return paths; } }
        public override IReadOnlyList<TimePath> PathsAsTime { get { return paths; } }

        public int nb_vehicules = 6121;

        private int sizeCoeff = 1;

        public string filename = "";

        public enum enumYAxis { zero, vehiculeSpeed, vehiculeDist };

        public enum enumColorAttribute { random, vehiculeSpeed, vehiculeDist };

        [SerializeField]
        private enumYAxis yAxis = enumYAxis.zero;
        private enumYAxis _yAxis;
        public enumYAxis YAxis {
            get => yAxis;
            set {
                if (_yAxis == value)
                    return;
                _yAxis = value;
                yAxis = _yAxis;
            }
        }

        [SerializeField]
        private enumColorAttribute colorAttribute = enumColorAttribute.random;
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
            districtSize = new Vector3(15, 15, 15);
        }


        public Vector3 maxPoint = new Vector3(100, 200, 2000);
        public Vector3 minPoint = new Vector3(-2000, -100, -100);

        protected override bool LoadFromFile() {

            Color32[] pathColors = new Color32[nb_vehicules];

            paths = new List<Lyon6Path>(nb_vehicules);
            for (int i = 0; i < nb_vehicules; i++) {
                GameObject go = new GameObject(i.ToString());
                go.transform.parent = transform;
                Lyon6Path p = go.AddComponent<Lyon6Path>();
                p.atoms = new List<Lyon6Atom>();
                paths.Add(p);
                pathColors[i] = Random.ColorHSV();
            }


            BinaryReader br = new BinaryReader(File.Open(filename, FileMode.Open));

            for (int i = 0; i < nb_vehicules; i++) {
                int natoms = br.ReadInt32();
                for (int atom_index = 0; atom_index < natoms; atom_index++) {

                    Vector3 point = new Vector3 {
                        x = -br.ReadSingle(),
                        z = br.ReadSingle()
                    };

                    float t = br.ReadSingle();
                    float dist = br.ReadSingle();
                    float velocity = br.ReadSingle();


                    if (YAxis == enumYAxis.vehiculeDist)
                        point.y = dist;
                    else if (YAxis == enumYAxis.vehiculeSpeed)
                        point.y = velocity * 3;
                    else
                        point.y = 0.5f;

                    point *= sizeCoeff;

                    point = Vector3.Max(point, minPoint);
                    point = Vector3.Min(point, maxPoint);

                    Lyon6Atom a = new Lyon6Atom() {
                        time = t,
                        point = point,
                        path = paths[i],
                        indexInPath = atom_index,
                        speed = velocity,
                        dist = dist
                    };

                    if (ColorAttribute == enumColorAttribute.vehiculeSpeed)
                        a.BaseColor = Color.Lerp(Color.blue, Color.red, a.speed / 15f);
                    else if (ColorAttribute == enumColorAttribute.vehiculeDist)
                        a.BaseColor = Color.Lerp(Color.blue, Color.red, (a.dist - 2.5f) / (90f - 2.5f));
                    else
                        a.BaseColor = pathColors[i];
                    
                    paths[i].atoms.Add(a);
                }
            }
            return true;
        }

        protected override float InterpretTime(string word) {
            return 0;
        }

        public class Lyon6Path : TimePath {
            public List<Lyon6Atom> atoms = new List<Lyon6Atom>();
            public override IReadOnlyList<Atom> AtomsAsBase { get { return atoms; } }
            public override IReadOnlyList<TimeAtom> AtomsAsTime { get { return atoms; } }
        }

        public class Lyon6Atom : TimeAtom {
            public float speed;
            public float dist;
        }
    }

}