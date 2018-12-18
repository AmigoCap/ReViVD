using UnityEngine;
using System.Collections.Generic;

public abstract class Visualization : MonoBehaviour {
    public Material material;
    public Vector3 districtSize = new Vector3(30, 30, 30);

    public bool getDebugData = false;

    protected abstract bool LoadFromCSV(string filename);

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

    protected void InitializeRendering() {
        foreach (Path p in PathsAsBase) {
            GameObject o = new GameObject(p.ID);
            o.transform.parent = transform;
            MeshFilter filter = o.AddComponent<MeshFilter>();
            p.mesh = new Mesh();
            filter.sharedMesh = p.mesh;
            MeshRenderer renderer = o.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            p.GenerateMesh();
        }

        CreateDistricts();

        Debug.Log(lowerBoundary);
    }

    protected void UpdateRendering() {
        foreach (Path p in PathsAsBase) {
            p.UpdateVertices();
        }

        if (getDebugData) {
            getDebugData = false;
            int[] c = GetCameraDistrict();
            Debug.Log(c[0].ToString() + '\t' + c[1].ToString() + '\t' + c[2].ToString());
        }
    }

    public abstract IReadOnlyList<Path> PathsAsBase { get; }

    private struct District { //Subdivision discrète de la visualisation dans l'espace pour optimisation
        public Atom[] atoms;
        public Path[] paths;
        public Vector3 center;
    }

    private Vector3 lowerBoundary; //Coordonnées de l'atome de coordonnées minimales
    private Vector3 upperBoundary; //Coordonnées de l'atome de coordonnées maximales

    private District[,,] districts; //Array tridimensionnel de Districts

    private void CalculateBoundaries() {
        lowerBoundary = PathsAsBase[0].AtomsAsBase[0].point;
        upperBoundary = PathsAsBase[0].AtomsAsBase[0].point;
        foreach (Path p in PathsAsBase) {
            foreach (Atom a in p.AtomsAsBase) {
                lowerBoundary = Vector3.Min(lowerBoundary, a.point);
                upperBoundary = Vector3.Max(upperBoundary, a.point);
            }
        }
    }

    private void CreateDistricts() { //Crée et remplit districts
        CalculateBoundaries();
        int[] numberOfDistricts = {
            (int)((upperBoundary.x - lowerBoundary.x) / districtSize.x + 1),
            (int)((upperBoundary.y - lowerBoundary.y) / districtSize.y + 1),
            (int)((upperBoundary.z - lowerBoundary.z) / districtSize.z + 1) };

        List<Atom>[,,] atomRepartition = new List<Atom>[numberOfDistricts[0], numberOfDistricts[1], numberOfDistricts[2]];
        HashSet<Path>[,,] pathsRepartition = new HashSet<Path>[numberOfDistricts[0], numberOfDistricts[1], numberOfDistricts[2]];
        for (int i = 0; i < numberOfDistricts[0]; i++) {
            for (int j = 0; j < numberOfDistricts[1]; j++) {
                for (int k = 0; k < numberOfDistricts[2]; k++) {
                    atomRepartition[i, j, k] = new List<Atom>();
                    pathsRepartition[i, j, k] = new HashSet<Path>();
                }
            }
        }
    
        foreach (Path p in PathsAsBase) {
            foreach (Atom a in p.AtomsAsBase) {
                int[] district = {
                    (int)((a.point.x - lowerBoundary.x) / districtSize.x),
                    (int)((a.point.y - lowerBoundary.y) / districtSize.y),
                    (int)((a.point.z - lowerBoundary.z) / districtSize.z)
                };
                atomRepartition[district[0], district[1], district[2]].Add(a);
                pathsRepartition[district[0], district[1], district[2]].Add(p);
            }
        }

        districts = new District[numberOfDistricts[0], numberOfDistricts[1], numberOfDistricts[2]];

        for (int x = 0; x < numberOfDistricts[0]; x++) {
            for (int y = 0; y < numberOfDistricts[1]; y++) {
                for (int z = 0; z < numberOfDistricts[2]; z++) {
                    districts[x, y, z] = new District {
                        atoms = atomRepartition[x, y, z].ToArray(),
                        center = new Vector3(districtSize.x * x, districtSize.y * y, districtSize.z * z) + districtSize / 2,
                        paths = new Path[pathsRepartition.Length]
                    };
                    pathsRepartition[x, y, z].CopyTo(districts[x, y, z].paths);
                }
            }
        }
    }

    private int[] GetCameraDistrict() {
        Vector3 pos = Camera.main.transform.position - lowerBoundary;
        int[] coords = {
            (int) (pos.x / districtSize.x),
            (int) (pos.y / districtSize.y),
            (int) (pos.z / districtSize.z)};
        return coords;
    }
}

public abstract class Path {
    public string ID;
    public Mesh mesh;
    public Dictionary<int, float> specialRadii = new Dictionary<int, float>();
    public float baseRadius = 0.1f;
    public float maxHeight = 400f;

    protected void CleanspecialRadii() {
        int AtomCount = AtomsAsBase.Count;
        List<int> keysToRemove = new List<int>();
        foreach (KeyValuePair<int, float> pair in specialRadii) {
            if (pair.Key > AtomCount - 2 || pair.Key < 0)
                keysToRemove.Add(pair.Key);
        }
        foreach (int key in keysToRemove)
            specialRadii.Remove(key);
    }

    public void GenerateMesh() {
        mesh.Clear();

        CleanspecialRadii();
        int AtomCount = AtomsAsBase.Count;

        int specialRadiiBonusTriangles = specialRadii.Count * 12
                                         - (specialRadii.ContainsKey(0) ? 6 : 0)
                                         - (specialRadii.ContainsKey(AtomCount - 2) ? 6 : 0);

        Vector3[] vertices = new Vector3[AtomCount * 5 - 6];
        Color32[] colors = new Color32[vertices.Length];
        int[] triangles = new int[AtomCount * 12 - 18 + specialRadiiBonusTriangles];

        int[] generator = { 0, 2, 1, 1, 2, 3, 2, 5, 4, 3, 4, 6 };
        for (int i = 0; i < triangles.Length - specialRadiiBonusTriangles; i++) {
            triangles[i] = generator[i % 12] + 5 * (i / 12);
        }

        int bonus_i = triangles.Length - specialRadiiBonusTriangles;
        int[] bonusGenerator = { 2, 4, 5, 3, 6, 5 };
        foreach (KeyValuePair<int, float> pair in specialRadii) {
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
        Vector3 currentPoint = AtomsAsBase[0].point;
        Vector3 nextPoint;
        Color32 pointColor = Color32.Lerp(new Color32(255, 0, 0, 255), new Color32(0, 0, 255, 255), currentPoint.y / maxHeight);
        for (int p = 0; p < atomCount - 1; p++) {
            nextPoint = AtomsAsBase[p+1].point;

            int i = 5 * p;
            float radius;
            if (!specialRadii.TryGetValue(p, out radius))
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

    public abstract IReadOnlyList<Atom> AtomsAsBase { get; }
}

public abstract class Atom {
    public Vector3 point;
}



public abstract class TimeVisualization : Visualization {
    protected abstract float InterpretTime(string word);

    public abstract IReadOnlyList<TimePath> PathsAsTime { get; }
}

public abstract class TimePath : Path {
    public abstract IReadOnlyList<TimeAtom> AtomsAsTime { get; }
}

public abstract class TimeAtom : Atom {
    public float time;
}