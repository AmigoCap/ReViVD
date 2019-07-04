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

        public string filename;

        public enum enumYAxis { zero, playingTime, playerSpeed, playerAcceleration, playerOdometer, playerHeartRate, playerChargeLoad};

        public enum enumColorAttribute { random, playingTime, playerSpeed, playerAcceleration, playerOdometer, playerHeartRate, playerChargeLoad };

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

        private float sizeCoeff = 2f;

        private Dictionary<int, int> substitutedPlayers = new Dictionary<int, int>();
        private Dictionary<int, int> substitutePlayers = new Dictionary<int, int>();

        public void Reset() {
            districtSize = new Vector3(15, 15, 15);
        }

        public Vector3 maxPoint = new Vector3(500, 500, 1500);
        public Vector3 minPoint = new Vector3(-500, -500, -200);

        protected override bool LoadFromFile() {
            //substituted and substitute players and time of the substitution
            substitutedPlayers.Add(10, 33);
            substitutedPlayers.Add(1, 47);
            substitutedPlayers.Add(2, 57);
            substitutedPlayers.Add(3, 57);
            substitutedPlayers.Add(8, 68);
            substitutedPlayers.Add(9, 74);

            substitutePlayers.Add(21, 33);
            substitutePlayers.Add(17, 47);
            substitutePlayers.Add(16, 57);
            substitutePlayers.Add(23, 57);
            substitutePlayers.Add(19, 68);
            substitutePlayers.Add(20, 74);


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
                long nextPathPosition = br.BaseStream.Position + pathLength * 8 * 4;

                int _instantsStart = instantsStart;
                int _pathLength = pathLength;

                while (currentPlayer < keptPlayers[i]) {
                    br.BaseStream.Position += pathLength * 8 * 4;
                    pathLength = br.ReadInt32();
                    currentPlayer++;
                }

                if (substitutedPlayers.ContainsKey(currentPlayer + 1)) {
                    _pathLength = (substitutedPlayers[currentPlayer + 1] + 10) * 60 * 10 * 8;  // (x minutes + 10 minutes d'échauffement) * 60 secondes * 10 mesures par seconde * 8 attributs
                }
                else if (substitutePlayers.ContainsKey(currentPlayer + 1)) { 
                    _instantsStart = substitutePlayers[currentPlayer + 1] * 60 * 10; // (x minutes) * 60 secondes * 10 mesures par seconde = environ début de son échauffement pour remplacer le joueur sur le terrain
                }

                if (_instantsStart + instantsStep >= _pathLength)
                    continue;
                int true_n_instants = Math.Min(_pathLength, _pathLength - _instantsStart);


                GameObject go = new GameObject((keptPlayers[i] + 1).ToString());
                go.transform.parent = transform;
                LouRugbyPath p = go.AddComponent<LouRugbyPath>();
                p.atoms = new List<LouRugbyAtom>(true_n_instants);
                Color32 color = UnityEngine.Random.ColorHSV();

               
                br.BaseStream.Position += _instantsStart * 8 * 4;

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
                    if (YAxis == enumYAxis.playerAcceleration)
                        point.y = acceleration;
                    else if (YAxis == enumYAxis.playerChargeLoad)
                        point.y = load;
                    else if (YAxis == enumYAxis.playerHeartRate)
                        point.y = heart;
                    else if (YAxis == enumYAxis.playerSpeed)
                        point.y = velocity;
                    else if (YAxis == enumYAxis.playerOdometer)
                        point.y = odometer;
                    else if (YAxis == enumYAxis.playingTime)
                        point.y = t / 20;
                    else
                        point.y = 0.5f;

                    point *= sizeCoeff;

                    point = Vector3.Max(point, minPoint);
                    point = Vector3.Min(point, maxPoint);


                    if (ColorAttribute == enumColorAttribute.playerAcceleration)
                        color = Color.Lerp(Color.blue, Color.red, (acceleration + 6.47294569f) / 20f);
                    else if (ColorAttribute == enumColorAttribute.playerChargeLoad)
                        color = Color.Lerp(Color.blue, Color.red, (load) / 816.1f);
                    else if (ColorAttribute == enumColorAttribute.playerHeartRate)
                        color = Color.Lerp(Color.blue, Color.red, heart);
                    else if (ColorAttribute == enumColorAttribute.playerSpeed)
                        color = Color.Lerp(Color.blue, Color.red, (velocity) / 43.2f);
                    else if (ColorAttribute == enumColorAttribute.playerOdometer)
                        color = Color.Lerp(Color.blue, Color.red, (odometer) / 7342.33f);


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