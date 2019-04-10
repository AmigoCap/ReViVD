using UnityEngine;
using System.Collections.Generic;

public class AirTrafficVisualization : TimeVisualization {
    public List<AirTrafficPath> paths;
    public override IReadOnlyList<Path> PathsAsBase { get { return paths; } }
    public override IReadOnlyList<TimePath> PathsAsTime { get { return paths; } }

    protected override bool LoadFromCSV(string filename) {
        districtSize = new Vector3(15, 15, 15);

        TextAsset file = Resources.Load<TextAsset>(filename);
        if (file == null)
            return false;
        paths = new List<AirTrafficPath>();
        Dictionary<string, AirTrafficPath> PathsDict = new Dictionary<string, AirTrafficPath>();

        string[] rawData = file.text.Split(new char[] { '\n' });

        foreach (string row in rawData) {
            string[] words = CsvSplit(row, ',');    //Selon configuration de l'OS, mettre ',' ou '.'

            if (words.Length < 2)
                continue;

            AirTrafficPath p;
            if (!PathsDict.TryGetValue(words[0], out p)) {
                p = new AirTrafficPath() { ID = words[0] };
                paths.Add(p);
                PathsDict.Add(p.ID, p);
            }

            AirTrafficAtom a = new AirTrafficAtom {
                time = InterpretTime(words[1]),
                point = new Vector3(float.Parse(words[2]), float.Parse(words[4]), float.Parse(words[3])),
                path = p,
                indexInPath = p.atoms.Count
            };
            a.baseColor = Color32.Lerp(new Color32(255, 0, 0, 255), new Color32(0, 0, 255, 255), a.point.y / 400f);

            p.atoms.Add(a);
        }

        return true;
    }

    protected override float InterpretTime(string word) {
        float time = 0;
        time += float.Parse(word.Substring(6, 6).Replace('.', ','));
        time += float.Parse(word.Substring(3, 2)) * 60;
        time += float.Parse(word.Substring(0, 2)) * 3600;
        return time;
    }

    bool doTime = false;

    private void Awake() {
        name = "airTraffic";

        if (!LoadFromCSV("data_aviation")) {
            return;
        }
    }

    private void Start() {
        AirTrafficPath p = paths[GetPathIndex("60")];
        int c = p.AtomsAsBase.Count;
        for (int i = 0; i < c; i += 2)
            p.specialRadii.Add(i, 0.3f);

        InitializeRendering();

        startTime = Time.time;
    }

    private float startTime = 0;

    private void Update() {
        if (Input.GetMouseButtonDown(0)) {
            startTime = Time.time;
        }
        if (Input.GetMouseButtonDown(1)) {
            doTime = !doTime;
            if (doTime) {
                startTime = Time.time;
            }
            else {
                foreach (AirTrafficPath p in paths) {
                    p.RemoveTimeWindow();
                }
            }
        }

        if (doTime) {
            foreach (AirTrafficPath p in paths) {
                p.SetTimeWindow((Time.time - startTime) * 60 - 300, (Time.time - startTime) * 60 + 300);
            }
        }

        UpdateRendering();
    }

    public class AirTrafficPath : TimePath {
        public List<AirTrafficAtom> atoms = new List<AirTrafficAtom>();
        public override IReadOnlyList<Atom> AtomsAsBase { get { return atoms; } }
        public override IReadOnlyList<TimeAtom> AtomsAsTime { get { return atoms; } }
    }

    public class AirTrafficAtom : TimeAtom {
    }
}
