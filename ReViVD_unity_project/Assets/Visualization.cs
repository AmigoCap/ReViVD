using UnityEngine;
using System.Collections.Generic;

public abstract class Visualization : MonoBehaviour {
    public Material material;

    public abstract bool LoadFromCSV(string filename);

    protected string[] CsvSplit(string row, char decimalChar = '.') { //En cas de CSV récalcitrant
        List<string> words = new List<string>();
        int i = 0;
        bool inQuotes = false;
        string currentWord = "";
        while (i < row.Length) {
            char c = row[i];
            if (c == ',') {
                if (inQuotes)
                    currentWord += decimalChar;
                else {
                    words.Add(currentWord);
                    currentWord = "";
                }
            }
            else if (c == '.')
                currentWord += decimalChar;
            else if (c == '"') {
                if (inQuotes) {
                    if (row[i + 1] == '"') { //"" entre d'autres guillemets correspond au symbole "
                        currentWord += '"';
                        i++;
                    }
                    else {
                        inQuotes = false;
                    }
                }
                else
                    inQuotes = true;
            }
            else {
                currentWord += c;
            }
            i++;
        }
        words.Add(currentWord);

        return words.ToArray();
    }

    public void InitializeRendering() {
        foreach (Path p in PathsAsBase) {
            GameObject o = new GameObject(p.ID);
            o.transform.parent = this.transform;
            MeshFilter filter = o.AddComponent<MeshFilter>();
            p.mesh = new Mesh();
            filter.sharedMesh = p.mesh;
            MeshRenderer renderer = o.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
        }
    }

    protected abstract IReadOnlyList<Path> PathsAsBase { get; }
}

public abstract class Path {
    public string ID { get; set; }
    public Mesh mesh;
    public Dictionary<int, float> SpecialRadii { get; set; } = new Dictionary<int, float>();
    public float baseRadius = 0.1f;
    public float maxHeight = 400f;

    protected void CleanSpecialRadii() {
        int AtomCount = AtomsAsBase.Count;
        List<int> keysToRemove = new List<int>();
        foreach (KeyValuePair<int, float> pair in SpecialRadii) {
            if (pair.Key > AtomCount - 2 || pair.Key < 0)
                keysToRemove.Add(pair.Key);
        }
        foreach (int key in keysToRemove)
            SpecialRadii.Remove(key);
    }

    public void GenerateMesh() {
        mesh.Clear();

        CleanSpecialRadii();
        int AtomCount = AtomsAsBase.Count;

        int specialRadiiBonusTriangles = SpecialRadii.Count * 12
                                         - (SpecialRadii.ContainsKey(0) ? 6 : 0)
                                         - (SpecialRadii.ContainsKey(AtomCount - 2) ? 6 : 0);

        Vector3[] vertices = new Vector3[AtomCount * 5 - 6];
        Color32[] colors = new Color32[vertices.Length];
        int[] triangles = new int[AtomCount * 12 - 18 + specialRadiiBonusTriangles];

        int[] generator = { 0, 2, 1, 1, 2, 3, 2, 5, 4, 3, 4, 6 };
        for (int i = 0; i < triangles.Length - specialRadiiBonusTriangles; i++) {
            triangles[i] = generator[i % 12] + 5 * (i / 12);
        }

        int bonus_i = triangles.Length - specialRadiiBonusTriangles;
        int[] bonusGenerator = { 2, 4, 5, 3, 6, 5 };
        foreach (KeyValuePair<int, float> pair in SpecialRadii) {
            int p = pair.Key;
            int start = bonus_i;
            if (p != 0) {
                while (bonus_i < start + 6) {
                    triangles[bonus_i] = bonusGenerator[bonus_i - start] + 5 * (p - 1);
                    bonus_i++;
                }
            }
            if (p != AtomCount - 2) {
                while (bonus_i < start + 6) {
                    triangles[bonus_i] = bonusGenerator[bonus_i - start] + 5 * p;
                    bonus_i++;
                }
            }
        }

        mesh.vertices = vertices;
        mesh.colors32 = colors;
        mesh.triangles = triangles;

        UpdateVertices();
    }

    public void UpdateVertices() {
        Vector3 camPos = Camera.main.transform.position;
        
        Vector3 vBase = new Vector3();

        Vector3[] vertices = mesh.vertices; //vertices et colors32 sont des propriétés et renvoient des copies
        Color32[] colors = mesh.colors32;

        int atomCount = AtomsAsBase.Count;
        Vector3 currentPoint = AtomsAsBase[0].Point;
        Vector3 nextPoint;
        Color32 pointColor = Color32.Lerp(new Color32(255, 0, 0, 255), new Color32(0, 0, 255, 255), currentPoint.y / maxHeight);
        for (int p = 0; p < atomCount - 1; p++) {
            nextPoint = AtomsAsBase[p+1].Point;

            int i = 5 * p;
            float radius;
            if (!SpecialRadii.TryGetValue(p, out radius))
                radius = baseRadius;

            vBase = radius * Vector3.Cross(nextPoint - camPos, nextPoint - currentPoint).normalized;
            vertices[i] = currentPoint + vBase;
            vertices[i+1] = currentPoint - vBase;
            vertices[i+2] = vertices[i] + nextPoint - currentPoint;
            vertices[i+3] = vertices[i + 1] + nextPoint - currentPoint;
            if (p < atomCount - 2)
                vertices[i+4] = nextPoint;

            colors[i] = pointColor;
            colors[i + 1] = pointColor;
            pointColor = Color32.Lerp(new Color32(255, 0, 0, 255), new Color32(0, 0, 255, 255), nextPoint.y / maxHeight);
            colors[i + 2] = pointColor;
            colors[i + 3] = pointColor;
            if (p < atomCount - 2)
                colors[i+4] = pointColor;

            currentPoint = nextPoint;
        }

        mesh.vertices = vertices;
        mesh.colors32 = colors;
        mesh.RecalculateBounds();
    }

    protected abstract IReadOnlyList<Atom> AtomsAsBase { get; }
}

public abstract class Atom {
    public Vector3 Point { get; set; }
}



public abstract class TimeVisualization : Visualization {
    protected abstract float InterpretTime(string word);

    protected abstract IReadOnlyList<TimePath> PathsAsTime { get; }
}

public abstract class TimePath : Path {
    protected abstract IReadOnlyList<TimeAtom> AtomsAsTime { get; }
}

public abstract class TimeAtom : Atom {
    public float Time { get; set; }
}