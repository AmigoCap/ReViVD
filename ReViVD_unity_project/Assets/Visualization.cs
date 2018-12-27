using UnityEngine;
using System.Collections.Generic;
using System;

public abstract class Visualization : MonoBehaviour {
    public Material material;
    public Vector3 districtSize = new Vector3(15, 15, 15);

    //TESTING
    public bool debugMode = false;
    public bool getDebugData = false;
    private List<int[]>[] districtsToHighlight = new List<int[]>[] { new List<int[]>(), new List<int[]>(), new List<int[]>() };

    private void OnDrawGizmos() {
        if (debugMode) {
            Gizmos.color = Color.green;
            Vector3 sides = new Vector3(districtSize.x * districts.GetLength(0), districtSize.y * districts.GetLength(1), districtSize.z * districts.GetLength(2));
            Gizmos.DrawWireCube(transform.TransformPoint(sides / 2 + lowerBoundary), sides);

            foreach (int[] d in districtsToHighlight[0]) {
                Gizmos.DrawWireCube(transform.TransformPoint(districtSize / 2 + lowerBoundary + new Vector3(d[0] * districtSize.x, d[1] * districtSize.y, d[2] * districtSize.z)), districtSize);
            }

            Gizmos.color = Color.red;
            foreach (int[] d in districtsToHighlight[1]) {
                Gizmos.DrawWireCube(transform.TransformPoint(districtSize / 2 + lowerBoundary + new Vector3(d[0] * districtSize.x, d[1] * districtSize.y, d[2] * districtSize.z)), districtSize);
            }

            Gizmos.color = Color.blue;
            foreach (int[] d in districtsToHighlight[2]) {
                Gizmos.DrawWireCube(transform.TransformPoint(districtSize / 2 + lowerBoundary + new Vector3(d[0] * districtSize.x, d[1] * districtSize.y, d[2] * districtSize.z)), districtSize);
            }
        }
    }

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
            p.transform = o.transform;
            MeshFilter filter = o.AddComponent<MeshFilter>();
            p.mesh = new Mesh();
            filter.sharedMesh = p.mesh;
            MeshRenderer renderer = o.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            p.GenerateMesh();
        }
        CreateDistricts();

        oldCameraDistrict = GetCameraDistrict();
        oldCameraDistrict[0] = oldCameraDistrict[0] + 10; //On prend une ancienne caméra différente de la véritable pour forcer une première update de tous les atomes
    }

    int[] oldCameraDistrict;
    HashSet<Path> pathsToUpdate = new HashSet<Path>();
    readonly bool[] thickTowardsPositive = { true, true, true }; //Définit la position du cube de 4x4 districts autour de la caméra

    protected void UpdateRendering() {
        int[] cameraDistrict = GetCameraDistrict();
        if (cameraDistrict[0] == oldCameraDistrict[0] && cameraDistrict[1] == oldCameraDistrict[1] && cameraDistrict[2] == oldCameraDistrict[2]) {
            foreach (Path p in pathsToUpdate) {
                p.UpdateVertices();
            }
        }
        else {
            bool shouldUpdateEverything = false;
            for (int i = 0; i < 3; i++) {
                if (cameraDistrict[i] == oldCameraDistrict[i] + (thickTowardsPositive[i] ? 1 : -1)) {
                    thickTowardsPositive[i] = !thickTowardsPositive[i];
                }
                else if (cameraDistrict[i] != oldCameraDistrict[i]) {
                    shouldUpdateEverything = true;
                }
            }

            if (shouldUpdateEverything) {
                foreach (Path p in pathsToUpdate) {
                    foreach (Atom a in p.AtomsAsBase) {
                        a.shouldUpdate = false;
                    }
                }
                pathsToUpdate.Clear();

                if (debugMode)
                    districtsToHighlight[0].Clear();

                for (int i = cameraDistrict[0] - (thickTowardsPositive[0] ? 1 : 2); i <= cameraDistrict[0] + (thickTowardsPositive[0] ? 2 : 1); i++) {
                    for (int j = cameraDistrict[1] - (thickTowardsPositive[1] ? 1 : 2); j <= cameraDistrict[1] + (thickTowardsPositive[1] ? 2 : 1); j++) {
                        for (int k = cameraDistrict[2] - (thickTowardsPositive[2] ? 1 : 2); k <= cameraDistrict[2] + (thickTowardsPositive[2] ? 2 : 1); k++) {
                            if (debugMode)
                                districtsToHighlight[0].Add(new int[3] { i, j, k });

                            try {
                                foreach (Atom a in districts[i, j, k].atoms) {
                                    a.shouldUpdate = true;
                                    pathsToUpdate.Add(a.path);
                                }
                            }
                            catch (System.IndexOutOfRangeException) {
                                continue;
                            }
                        }
                    }
                }

                foreach (Path p in PathsAsBase) {
                    p.UpdateVertices(true);
                }
            }
        }

        oldCameraDistrict = cameraDistrict;

        if (getDebugData) { //TESTING
            getDebugData = false;
        }
    }

    public abstract IReadOnlyList<Path> PathsAsBase { get; }

    private struct District { //Subdivision discrète de la visualisation dans l'espace pour optimisation
        public Atom[] atoms;
        public Vector3 center;
    }

    private Vector3 lowerBoundary; //Coordonnées de l'atome de coordonnées minimales
    private Vector3 upperBoundary; //Coordonnées de l'atome de coordonnées maximales

    private District[,,] districts; //Array tridimensionnel de Districts

    private void CalculateBoundaries() {
        lowerBoundary = transform.InverseTransformPoint(PathsAsBase[0].transform.TransformPoint(PathsAsBase[0].AtomsAsBase[0].point));
        upperBoundary = transform.InverseTransformPoint(PathsAsBase[0].transform.TransformPoint(PathsAsBase[0].AtomsAsBase[0].point));
        foreach (Path p in PathsAsBase) {
            foreach (Atom a in p.AtomsAsBase) {
                lowerBoundary = Vector3.Min(lowerBoundary, transform.InverseTransformPoint(p.transform.TransformPoint(a.point)));
                upperBoundary = Vector3.Max(upperBoundary, transform.InverseTransformPoint(p.transform.TransformPoint(a.point)));
            }
        }
    }

    private void CreateDistricts() { //Crée et remplit districts
        CalculateBoundaries();
        int[] numberOfDistricts = {
            Mathf.FloorToInt((upperBoundary.x - lowerBoundary.x) / districtSize.x + 1),
            Mathf.FloorToInt((upperBoundary.y - lowerBoundary.y) / districtSize.y + 1),
            Mathf.FloorToInt((upperBoundary.z - lowerBoundary.z) / districtSize.z + 1) };

        List<Atom>[,,] atomRepartition = new List<Atom>[numberOfDistricts[0], numberOfDistricts[1], numberOfDistricts[2]];
        for (int i = 0; i < numberOfDistricts[0]; i++) {
            for (int j = 0; j < numberOfDistricts[1]; j++) {
                for (int k = 0; k < numberOfDistricts[2]; k++) {
                    atomRepartition[i, j, k] = new List<Atom>();
                }
            }
        }

        foreach (Path p in PathsAsBase) {
            Vector3 point = transform.InverseTransformPoint(p.transform.TransformPoint(p.AtomsAsBase[0].point));

            for (int i = 0; i < p.AtomsAsBase.Count - 1; i++) {
                Vector3 nextPoint = transform.InverseTransformPoint(p.transform.TransformPoint(p.AtomsAsBase[i + 1].point));
                Vector3 delta = (nextPoint - point).normalized;

                //On détermine les intersections avec les bords de la visualisation
                Vector3 retroIntersect, proIntersect;
                Vector3 retroCoeffs = Vector3.negativeInfinity;
                Vector3 proCoeffs = Vector3.positiveInfinity;

                float temp;

                temp = (upperBoundary.x - point.x) / delta.x;
                if (temp > 0) proCoeffs.x = temp; else retroCoeffs.x = temp;
                temp = (upperBoundary.y - point.y) / delta.y;
                if (temp > 0) proCoeffs.y = temp; else retroCoeffs.y = temp;
                temp = (upperBoundary.z - point.z) / delta.z;
                if (temp > 0) proCoeffs.z = temp; else retroCoeffs.z = temp;

                temp = (lowerBoundary.x - point.x) / delta.x;
                if (temp > 0) proCoeffs.x = temp; else retroCoeffs.x = temp;
                temp = (lowerBoundary.y - point.y) / delta.y;
                if (temp > 0) proCoeffs.y = temp; else retroCoeffs.y = temp;
                temp = (lowerBoundary.z - point.z) / delta.z;
                if (temp > 0) proCoeffs.z = temp; else retroCoeffs.z = temp;

                proIntersect = Mathf.Min(proCoeffs.x, proCoeffs.y, proCoeffs.z) * delta + point;
                retroIntersect = Mathf.Max(retroCoeffs.x, retroCoeffs.y, retroCoeffs.z) * delta + point;

                //On traduit ces intersections en districts
                int[] retroDistrict = {
                    //On utilise Min et Max pour éviter les soucis avec les flottants (on est ici toujours à la frontière, donc il y en a !)
                    Math.Min(numberOfDistricts[0] - 1, Math.Max(0, Mathf.FloorToInt((retroIntersect.x - lowerBoundary.x) / districtSize.x))),
                    Math.Min(numberOfDistricts[1] - 1, Math.Max(0, Mathf.FloorToInt((retroIntersect.y - lowerBoundary.y) / districtSize.y))),
                    Math.Min(numberOfDistricts[2] - 1, Math.Max(0, Mathf.FloorToInt((retroIntersect.z - lowerBoundary.z) / districtSize.z)))
                };


                int[] proDistrict = {
                    Math.Min(numberOfDistricts[0] - 1, Math.Max(0, Mathf.FloorToInt((proIntersect.x - lowerBoundary.x) / districtSize.x))),
                    Math.Min(numberOfDistricts[1] - 1, Math.Max(0, Mathf.FloorToInt((proIntersect.y - lowerBoundary.y) / districtSize.y))),
                    Math.Min(numberOfDistricts[2] - 1, Math.Max(0, Mathf.FloorToInt((proIntersect.z - lowerBoundary.z) / districtSize.z)))
                };

                //Algorithme de Bresenham en 3D : on détermine tous les districts entre ces deux intersections
                atomRepartition[retroDistrict[0], retroDistrict[1], retroDistrict[2]].Add(p.AtomsAsBase[i]);

                int Dx = Math.Abs(proDistrict[0] - retroDistrict[0]), Dy = Math.Abs(proDistrict[1] - retroDistrict[1]), Dz = Math.Abs(proDistrict[2] - retroDistrict[2]);
                int xs = proDistrict[0] > retroDistrict[0] ? 1 : -1, ys = proDistrict[1] > retroDistrict[1] ? 1 : -1, zs = proDistrict[2] > retroDistrict[2] ? 1 : -1;

                if (Dx >= Dy && Dx >= Dz) {
                    int p1 = 2 * Dy - Dx, p2 = 2 * Dz - Dx;
                    while (retroDistrict[0] != proDistrict[0]) {
                        retroDistrict[0] += xs;
                        if (p1 >= 0) {
                            retroDistrict[1] += ys;
                            p1 -= 2 * Dx;
                        }
                        if (p2 >= 0) {
                            retroDistrict[2] += zs;
                            p2 -= 2 * Dx;
                        }
                        p1 += 2 * Dy;
                        p2 += 2 * Dz;
                        atomRepartition[retroDistrict[0], retroDistrict[1], retroDistrict[2]].Add(p.AtomsAsBase[i]);
                    }
                }
                else if (Dy >= Dx && Dy >= Dz) {
                    int p1 = 2 * Dx - Dy, p2 = 2 * Dz - Dy;
                    while (retroDistrict[1] != proDistrict[1]) {
                        retroDistrict[1] += ys;
                        if (p1 >= 0) {
                            retroDistrict[0] += xs;
                            p1 -= 2 * Dy;
                        }
                        if (p2 >= 0) {
                            retroDistrict[2] += zs;
                            p2 -= 2 * Dy;
                        }
                        p1 += 2 * Dx;
                        p2 += 2 * Dz;
                        atomRepartition[retroDistrict[0], retroDistrict[1], retroDistrict[2]].Add(p.AtomsAsBase[i]);
                    }
                }
                else {
                    int p1 = 2 * Dy - Dz, p2 = 2 * Dx - Dz;
                    while (retroDistrict[2] != proDistrict[2]) {
                        retroDistrict[2] += zs;
                        if (p1 >= 0) {
                            retroDistrict[1] += ys;
                            p1 -= 2 * Dz;
                        }
                        if (p2 >= 0) {
                            retroDistrict[0] += xs;
                            p2 -= 2 * Dz;
                        }
                        p1 += 2 * Dy;
                        p2 += 2 * Dx;
                        atomRepartition[retroDistrict[0], retroDistrict[1], retroDistrict[2]].Add(p.AtomsAsBase[i]);
                    }
                }

                point = nextPoint;
            }
        }

        districts = new District[numberOfDistricts[0], numberOfDistricts[1], numberOfDistricts[2]];

        for (int x = 0; x < numberOfDistricts[0]; x++) {
            for (int y = 0; y < numberOfDistricts[1]; y++) {
                for (int z = 0; z < numberOfDistricts[2]; z++) {
                    districts[x, y, z] = new District {
                        atoms = atomRepartition[x, y, z].ToArray(),
                        center = new Vector3(districtSize.x * x, districtSize.y * y, districtSize.z * z) + districtSize / 2,
                    };
                }
            }
        }
    }

    private int[] GetCameraDistrict() {
        Vector3 pos = transform.InverseTransformPoint(Camera.main.transform.position) - lowerBoundary;
        int[] coords = {
            Mathf.FloorToInt(pos.x / districtSize.x),
            Mathf.FloorToInt(pos.y / districtSize.y),
            Mathf.FloorToInt(pos.z / districtSize.z)};
        return coords;
    }
}

public abstract class Path {
    public string ID;
    public Mesh mesh;
    public Transform transform;
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

    public void UpdateVertices(bool forceUpdateAll = false) {
        Vector3 camPos = Camera.main.transform.position;
        
        Vector3 vBase = new Vector3();

        Vector3[] vertices = mesh.vertices; //vertices et colors32 sont des propriétés et renvoient des copies
        Color32[] colors = mesh.colors32;

        int atomCount = AtomsAsBase.Count;
        Vector3 currentPoint = AtomsAsBase[0].point;
        Vector3 nextPoint;
        Color32 pointColor = Color32.Lerp(new Color32(255, 0, 0, 255), new Color32(0, 0, 255, 255), currentPoint.y / maxHeight);

        for (int p = 0; p < atomCount - 1; p++) {
            if (!AtomsAsBase[p].shouldUpdate && !forceUpdateAll) {
                if (p == 0 || !AtomsAsBase[p - 1].shouldUpdate) {
                    pointColor = Color32.Lerp(new Color32(255, 0, 0, 255), new Color32(0, 0, 255, 255), AtomsAsBase[p + 1].point.y / maxHeight);
                    continue;
                }
            }

            currentPoint = AtomsAsBase[p].point;
            nextPoint = AtomsAsBase[p + 1].point;

            int i = 5 * p;
            float radius;
            if (!specialRadii.TryGetValue(p, out radius))
                radius = baseRadius;

            vBase = radius * Vector3.Cross(nextPoint - camPos, nextPoint - currentPoint).normalized;
            vertices[i] = currentPoint + vBase;
            vertices[i + 1] = currentPoint - vBase;
            vertices[i + 2] = vertices[i] + nextPoint - currentPoint;
            vertices[i + 3] = vertices[i + 1] + nextPoint - currentPoint;
            if (p < atomCount - 2)
                vertices[i + 4] = nextPoint;

            colors[i] = pointColor;
            colors[i + 1] = pointColor;
            pointColor = Color32.Lerp(new Color32(255, 0, 0, 255), new Color32(0, 0, 255, 255), nextPoint.y / maxHeight);

            colors[i + 2] = pointColor;
            colors[i + 3] = pointColor;
            if (p < atomCount - 2)
                colors[i+4] = pointColor;
        }

        mesh.vertices = vertices;
        mesh.colors32 = colors;
        mesh.RecalculateBounds();
    }

    public abstract IReadOnlyList<Atom> AtomsAsBase { get; }
}

public abstract class Atom {
    public Vector3 point;
    public Path path;
    public bool shouldUpdate = false;
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