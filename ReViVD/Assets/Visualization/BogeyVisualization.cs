using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

namespace Revivd {

    public class BogeyVisualization : TimeVisualization {
        public List<BogeyPath> paths;
        public override IReadOnlyList<Path> PathsAsBase { get { return paths; } }
        public override IReadOnlyList<TimePath> PathsAsTime { get { return paths; } }

        public int particlesStart = 0;
        public int n_particles = 1000;
        public int particlesStep = 1;
        public bool randomParticles = false;

        public int instantsStart = 0;
        public int n_instants = 50;
        public int instantsStep = 1;

        private float sizeCoeff = 0.5f;

        public void Reset() {
            districtSize = new Vector3(50, 50, 100);
        }

        public string filename;

        public Vector3 maxPoint = new Vector3(500, 500, 1500);
        public Vector3 minPoint = new Vector3(-500, -500, -200);

        protected override bool LoadFromFile() {
            int[] keptParticles = new int[n_particles];

            Tools.StartClock();

            if (randomParticles) {
                SortedSet<int> chosenRandomParticles = new SortedSet<int>(); //keptParticles should always be sorted
                System.Random rnd = new System.Random();
                for (int i = 0; i < n_particles; i++) {
                    while (!chosenRandomParticles.Add(rnd.Next(1000000))) { }
                }
                chosenRandomParticles.CopyTo(keptParticles);
            }
            else {
                for (int i = 0; i < n_particles; i++)
                    keptParticles[i] = particlesStart + particlesStep * i;
            }

            Tools.AddClockStop("generated particles array");

            paths = new List<BogeyPath>(n_particles);
            
            int instant = instantsStart;
            BinaryReader br = new BinaryReader(File.Open(filename, FileMode.Open));
            Tools.AddClockStop("Loaded data file");

            int currentParticle = 0;
            for (int i = 0; i < keptParticles.Length; i++) {
                if (br.BaseStream.Position == br.BaseStream.Length) {
                    Debug.Log("Reached EoF on loading paths after " + paths.Count + " paths");
                    break;
                }

                int pathLength = br.ReadInt32();
                while (currentParticle < keptParticles[i]) {
                    br.BaseStream.Position += pathLength * 3 * 4;
                    pathLength = br.ReadInt32();
                    currentParticle++;
                }

                GameObject go = new GameObject(keptParticles[i].ToString());
                go.transform.parent = transform;
                BogeyPath p = go.AddComponent<BogeyPath>();
                p.atoms = new List<BogeyAtom>(pathLength);
                Color32 color = Random.ColorHSV();

                for (int j = 0; j < pathLength; j++) {
                    Vector3 point = new Vector3();
                    point.x = br.ReadSingle() * sizeCoeff;
                    point.y = br.ReadSingle() * sizeCoeff;
                    point.z = br.ReadSingle() * sizeCoeff;

                    point = Vector3.Max(point, minPoint);
                    point = Vector3.Min(point, maxPoint);

                    p.atoms.Add(new BogeyAtom() {
                        time = (float)j,
                        point = point,
                        path = p,
                        indexInPath = j,
                        BaseColor = color
                    });
                }
                paths.Add(p);
                currentParticle++;
            }

            Tools.EndClock("Loaded paths");
            return true;
        }

        protected override float InterpretTime(string word) {
            return 0;
        }

        public class BogeyPath : TimePath {
            public List<BogeyAtom> atoms = new List<BogeyAtom>();
            public override IReadOnlyList<Atom> AtomsAsBase { get { return atoms; } }
            public override IReadOnlyList<TimeAtom> AtomsAsTime { get { return atoms; } }
        }

        public class BogeyAtom : TimeAtom {
        }
    }

}