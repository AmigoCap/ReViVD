using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;

namespace Revivd {

    public class LouRugbyVisualization : TimeVisualization {
        public List<LouRugbyPath> paths;
        public override IReadOnlyList<Path> PathsAsBase { get { return paths; } }
        public override IReadOnlyList<TimePath> PathsAsTime { get { return paths; } }

        public int nb_players = 26;

        public int instantsStart = 0;
        public int instantsStep = 1;

        private float sizeCoeff = 2f;

        public void Reset() {
            districtSize = new Vector3(15, 15, 15);
        }

        public string filename;

        public Vector3 maxPoint = new Vector3(500, 500, 1500);
        public Vector3 minPoint = new Vector3(-500, -500, -200);

        protected override bool LoadFromFile() {
            Tools.StartClock();
            Tools.SetGPSOrigin(new Vector2(45.72377830692287f, 4.8322574249907895f)); //set Unity Origin to be the center of the rugby field

            int[] keptPlayers = new int[nb_players];
            instantsStep = Math.Max(instantsStep, 1);

            for (int i = 0; i < nb_players; i++)
                keptPlayers[i] = i;

            Tools.AddClockStop("generated players array");

            paths = new List<LouRugbyPath>(nb_players);

            int instant = instantsStart;
            BinaryReader br = new BinaryReader(File.Open(filename, FileMode.Open));
            Tools.AddClockStop("Loaded data file");

            int currentPlayer = 0;
            for (int i = 0; i < keptPlayers.Length; i++) {
                if (br.BaseStream.Position == br.BaseStream.Length) {
                    Debug.Log("Reached EoF on loading paths after " + paths.Count + " paths");
                    break;
                }

                int pathLength = br.ReadInt32();

                while (currentPlayer < keptPlayers[i]) {
                    br.BaseStream.Position += pathLength * 8 * 4;
                    pathLength = br.ReadInt32();
                    currentPlayer++;
                }

                if (instantsStart + instantsStep >= pathLength)
                    continue;
                int true_n_instants = Math.Min(pathLength, pathLength - instantsStart);
                

                GameObject go = new GameObject((keptPlayers[i]+1).ToString());
                go.transform.parent = transform;
                LouRugbyPath p = go.AddComponent<LouRugbyPath>();
                p.atoms = new List<LouRugbyAtom>(true_n_instants);
                Color32 color = UnityEngine.Random.ColorHSV();

                long nextPathPosition = br.BaseStream.Position + pathLength * 8 * 4;
                br.BaseStream.Position += instantsStart * 8 * 4;
                
                for (int j = 0; j < true_n_instants; j += instantsStep) {

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
                    

                    p.atoms.Add(new LouRugbyAtom() {
                        time = t, //(float)(j + instantsStart)
                        point = point,
                        path = p,
                        indexInPath = j / instantsStep,
                        speed = velocity,
                        acceleration = acceleration,
                        odometer = odometer,
                        heart_rate = heart,
                        player_load = load,
                        BaseColor = color
                    });

                    br.BaseStream.Position += (instantsStep - 1) * 8 * 4;
                }

                br.BaseStream.Position = nextPathPosition;

                paths.Add(p);
                currentPlayer++;
            }

            Tools.EndClock("Loaded paths");
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