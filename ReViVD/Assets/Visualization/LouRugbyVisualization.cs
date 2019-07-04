using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace Revivd {

    public class LouRugbyVisualization : TimeVisualization {
        public List<LouRugbyPath> paths;
        public override IReadOnlyList<Path> PathsAsBase { get { return paths; } }
        public override IReadOnlyList<TimePath> PathsAsTime { get { return paths; } }

        public string filename = "";

        public int nb_players = 26;

        private int sizeCoeff = 2;

        public void Reset() {
            districtSize = new Vector3(15, 15, 15);
        }


        public Vector3 maxPoint = new Vector3(2000, 2000, 2000);
        public Vector3 minPoint = new Vector3(-2000, -2000, -2000);

        protected override bool LoadFromFile() {

            Tools.SetGPSOrigin(new Vector2(45.72377830692287f, 4.8322574249907895f));
            //Debug.Log(2*Tools.GPSToXYZ(new Vector2(45.72321854114279f, 4.832329569969196f)));
            //Debug.Log(2*Tools.GPSToXYZ(new Vector2(45.72405377738516f, 4.832959767816362f)));
            //Debug.Log(2*Tools.GPSToXYZ(new Vector2(45.7243368966102f, 4.83218369263958f)));
            //Debug.Log(2*Tools.GPSToXYZ(new Vector2(45.72350401255333f, 4.83155666953802f)));

            Color32[] pathColors = new Color32[nb_players];

            paths = new List<LouRugbyPath>(nb_players);
            for (int i = 0; i < nb_players; i++) {
                GameObject go = new GameObject((i+1).ToString());
                go.transform.parent = transform;
                LouRugbyPath p = go.AddComponent<LouRugbyPath>();
                p.atoms = new List<LouRugbyAtom>();
                paths.Add(p);
                pathColors[i] = Random.ColorHSV();
            }


            BinaryReader br = new BinaryReader(File.Open(filename, FileMode.Open));

            for (int i = 0; i < nb_players; i++) {
                int natoms = br.ReadInt32();
                for (int atom_index = 0; atom_index < natoms; atom_index++) {
                    float t = br.ReadSingle();
                    float velocity = br.ReadSingle();
                    float acceleration = br.ReadSingle();
                    float odometer = br.ReadSingle();
                    float latitude = br.ReadSingle();
                    float longitude = br.ReadSingle();
                    float heart = br.ReadSingle();
                    float load = br.ReadSingle();


                    Vector3 point = Tools.GPSToXYZ(new Vector2(latitude, longitude));

                    point.y = heart;
                    point *= sizeCoeff;

                    point = Vector3.Max(point, minPoint);
                    point = Vector3.Min(point, maxPoint);

                    LouRugbyAtom a = new LouRugbyAtom() {
                        time = t,
                        point = point,
                        path = paths[i],
                        indexInPath = atom_index,
                        speed = velocity,
                        acceleration = acceleration,
                        odometer = odometer,
                        heart_rate = heart,
                        player_load = load
                    };

                    a.BaseColor = pathColors[i];

                    paths[i].atoms.Add(a);
                }
            }
            return true;
        }

        protected override float InterpretTime(string word) {
            return 0;
        }

        public class LouRugbyPath : TimePath {
            public List<LouRugbyAtom> atoms = new List<LouRugbyAtom>();
            public override IReadOnlyList<Atom> AtomsAsBase { get { return atoms; } }
            public override IReadOnlyList<TimeAtom> AtomsAsTime { get { return atoms; } }
        }

        public class LouRugbyAtom : TimeAtom {
            public float speed;
            public float acceleration;
            public float odometer;
            public float heart_rate;
            public float player_load;
        }
    }

}