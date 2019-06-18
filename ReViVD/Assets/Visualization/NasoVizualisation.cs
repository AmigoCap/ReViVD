using UnityEngine;
using System.Collections.Generic;
using System.IO;

namespace Revivd {

    public class NasoVizualisation : TimeVisualization {
        public List<NasoPath> paths;
        public override IReadOnlyList<Path> PathsAsBase { get { return paths; } }
        public override IReadOnlyList<TimePath> PathsAsTime { get { return paths; } }

        public string filenameBase = "";

        public int particlesStart = 0;
        public int n_particles = 1000;
        public int particlesStep = 1;
        public bool randomParticles = false;

        public int instantsStart = 0;
        public int n_instants = 50;
        public int instantsStep = 1;

        private int sizeCoeff = 50;

        public void Reset() {
            districtSize = new Vector3(15, 15, 15);
        }


        protected override bool LoadFromFile() {
            if (filenameBase == "")
                return false;

            int[] keptParticles = new int[n_particles];
            Color32[] pathColors = new Color32[n_particles];

            Tools.StartClock();

            if (randomParticles) {
                SortedSet<int> chosenRandomParticles = new SortedSet<int>();
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

            paths = new List<NasoPath>(n_particles);
            for (int i = 0; i < n_particles; i++) {
                GameObject go = new GameObject(keptParticles[i].ToString());
                go.transform.parent = transform;
                paths.Add(go.AddComponent<NasoPath>());
                pathColors[i] = Random.ColorHSV();
            }

            Tools.AddClockStop("created paths");

            int currentFileNumber = instantsStart / n_instants + 1;
            int instant = instantsStart;
            int localInstant = instantsStart % 50;
            TextAsset currentFile = Resources.Load<TextAsset>(filenameBase + currentFileNumber.ToString("0000"));
            Stream s = new MemoryStream(currentFile.bytes);
            BinaryReader br = new BinaryReader(s);
            Tools.AddClockStop("Loaded file " + currentFileNumber.ToString("0000"));
            for (int t = 0; t < n_instants; t++) {
                if (localInstant >=  50) {
                    Tools.AddClockStop("Finished loading from file " + currentFileNumber.ToString("0000"));

                    localInstant %= 50;
                    br.Close();
                    s.Close();
                    Resources.UnloadAsset(currentFile);
                    Tools.AddClockStop("Closed file " + currentFileNumber.ToString("0000"));

                    currentFileNumber++;
                    currentFile = Resources.Load<TextAsset>(filenameBase + currentFileNumber.ToString("0000"));
                    Tools.AddSubClockStop("Resources.Load");
                    s = new MemoryStream(currentFile.bytes);
                    Tools.AddSubClockStop("MemoryStream creation");
                    br = new BinaryReader(s);
                    Tools.AddSubClockStop("BinaryReader creation");

                    Tools.AddClockStop("Loaded file " + currentFileNumber.ToString("0000"));
                }

                for (int i = 0; i < n_particles; i++) {
                    Vector3 point = new Vector3();
                    s.Position = (3 * 1000000 * localInstant + keptParticles[i]) * 8;
                    point.x = ((float)br.ReadDouble() - 317) * sizeCoeff;
                    s.Position += 1000000 * 8;
                    point.z = ((float)br.ReadDouble() - 317) * sizeCoeff;
                    s.Position += 1000000 * 8;
                    point.y = ((float)br.ReadDouble() - 317) * sizeCoeff;

                    NasoAtom a = new NasoAtom() {
                        time = (float)instant,
                        point = point,
                        path = paths[i],
                        indexInPath = t
                    };

                    a.BaseColor = pathColors[i];
                    paths[i].atoms.Add(a);
                }

                Tools.AddSubClockStop("Loaded particles for instant " + instant);

                instant += instantsStep;
                localInstant += instantsStep;
            }

            Tools.EndClock();

            return true;
        }

        /*
        protected override bool LoadFromFile() {
            UnityEditor.SceneView.FocusWindowIfItsOpen(typeof(UnityEditor.SceneView)); //DEBUG
            if (dataFile == null)
                return false;

            Stream s = new MemoryStream(dataFile.bytes);
            BinaryReader br = new BinaryReader(s);

            System.Random rnd = new System.Random();
            HashSet<int> randomNumbersGenerated = new HashSet<int>();

            paths = new List<NasoPath>(n_particles);
            for (int i = 0; i < n_particles; i++) {
                int true_i;
                if (randomParticles) {
                    do {
                        true_i = rnd.Next(1000000);
                    } while (!randomNumbersGenerated.Add(true_i));
                }
                else {
                    true_i = particlesStart + i * particlesStep;
                }

                GameObject go = new GameObject(true_i.ToString());
                go.transform.parent = transform;
                NasoPath p = go.AddComponent<NasoPath>();

                Color32 color = Random.ColorHSV();

                for (int t = 0; t < n_instants; t++) {
                    int true_t = instantsStart + instantsStep * t;

                    Vector3 point = new Vector3();
                    s.Position = (3 * 1000000 * true_t + true_i)*8;
                    point.x = (float)br.ReadDouble() * sizeCoeff;
                    s.Position += 1000000 * 8;
                    point.z = (float)br.ReadDouble() * sizeCoeff;
                    s.Position += 1000000 * 8;
                    point.y = (float)br.ReadDouble() * sizeCoeff;

                    NasoAtom a = new NasoAtom() {
                        time = (float)true_t,
                        point = point,
                        path = p,
                        indexInPath = t
                    };

                    a.BaseColor = color;
                    p.atoms.Add(a);
                }

                paths.Add(p);
            }

            return true;
        } */

        protected override float InterpretTime(string word) {
            return 0;
        }

        public class NasoPath : TimePath {
            public List<NasoAtom> atoms = new List<NasoAtom>();
            public override IReadOnlyList<Atom> AtomsAsBase { get { return atoms; } }
            public override IReadOnlyList<TimeAtom> AtomsAsTime { get { return atoms; } }
        }

        public class NasoAtom : TimeAtom {
        }
    }

}