using UnityEngine;
using System.Collections.Generic;
using System.Globalization;

public class LouRugbyVisualization : TimeVisualization {
    public List<LouRugbyPath> Paths { get; set; }
    public override IReadOnlyList<Path> PathsAsBase { get { return Paths; } }
    public override IReadOnlyList<TimePath> PathsAsTime { get { return Paths; } }

    protected override bool LoadFromCSV(string filename) {
        districtSize = new Vector3(3, 3, 3);

        TextAsset file = Resources.Load<TextAsset>(filename);
        if (file == null)
            return false;
        Paths = new List<LouRugbyPath>();
        Dictionary<string, LouRugbyPath> PathsDict = new Dictionary<string, LouRugbyPath>();

        string[] rawData = file.text.Split(new char[] { '\n' });

        foreach (string row in rawData) {
            string[] words = CsvSplit(row, ',');    //Selon configuration de l'OS, mettre ',' ou '.'

            if (words.Length < 2)
                continue;

            LouRugbyPath p;
            if (!PathsDict.TryGetValue(words[0], out p)) {
                p = new LouRugbyPath() { ID = words[0], baseRadius = 0.001f };
                Paths.Add(p);
                PathsDict.Add(p.ID, p);
            }

            LouRugbyAtom a = new LouRugbyAtom {
                time = InterpretTime(words[4]),
                point = new Vector3(float.Parse(words[1]), float.Parse(words[3]), float.Parse(words[2])),
                path = p
            };
            p.atoms.Add(a);
        }

        return true;
    }

    protected override float InterpretTime(string word) {
        float time = float.Parse(word.Replace('.', ','));
        return time;
    }

    bool doTime = false;

    private void Start() {
        if (!LoadFromCSV("data_lourugby")) {
            return;
        }
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
                foreach (LouRugbyPath p in Paths) {
                    p.RemoveTimeWindow();
                }
            }
        }

        if (doTime) {
            foreach (LouRugbyPath p in Paths) {
                p.SetTimeWindow((Time.time - startTime) * 60 - 300, (Time.time - startTime) * 60 + 300);
            }
        }

        UpdateRendering();
    }

    public class LouRugbyPath : TimePath {
        public List<LouRugbyAtom> atoms = new List<LouRugbyAtom>();
        public override IReadOnlyList<Atom> AtomsAsBase { get { return atoms; } }
        public override IReadOnlyList<TimeAtom> AtomsAsTime { get { return atoms; } }
    }

    public class LouRugbyAtom : TimeAtom {

    }
}
