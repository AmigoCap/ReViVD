using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Revivd {

    [DisallowMultipleComponent]
    public abstract class Path : MonoBehaviour {
        public Dictionary<int, float> specialRadii = new Dictionary<int, float>();
        public float baseRadius = 0.1f;

        public abstract IReadOnlyList<Atom> AtomsAsBase { get; }

        public bool needsMeshUpdate = false; //Rebuild the whole mesh from scratch

        public bool needsTriangleUpdate = false; //Update which atoms are displayed

        public bool needsVerticesUpdate = false; //Update the position of the vertices
        public bool forceFullVerticesUpdate = false;

        public bool needsColorUpdate = false; //Update the color of the vertices

        protected virtual void Awake() {
            MeshFilter filter = gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = new Mesh();
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = Visualization.Instance.material;
            needsMeshUpdate = true;
        }

        protected virtual void Update() {
            if (needsMeshUpdate)
                UpdateMesh();
            if (needsVerticesUpdate)
                UpdateVertices();
            if (needsColorUpdate)
                UpdateColor();
            if (needsTriangleUpdate)
                UpdateTriangles();
        }

        private void UpdateMesh() {
            Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
            mesh.Clear();

            int AtomCount = AtomsAsBase.Count;

            Vector3[] vertices = new Vector3[AtomCount * 5 - 6];
            Color32[] colors = new Color32[vertices.Length];

            mesh.vertices = vertices;
            mesh.colors32 = colors;

            needsMeshUpdate = false;
            needsVerticesUpdate = true;
            needsTriangleUpdate = true;
            needsColorUpdate = true;
        }

        private void UpdateVertices() {
            Mesh mesh = GetComponent<MeshFilter>().sharedMesh;
            Vector3 camPos = Camera.main.transform.position;

            Vector3 vBase = new Vector3();

            Vector3[] vertices = mesh.vertices; //vertices et colors32 sont des propriétés et renvoient des copies

            int atomCount = AtomsAsBase.Count;

            for (int p = 0; p < atomCount - 1; p++) {
                Atom currentAtom = AtomsAsBase[p];

                if (!forceFullVerticesUpdate || !currentAtom.ShouldDisplay || (!currentAtom.ShouldUpdateVertices && (p == 0 || !AtomsAsBase[p - 1].ShouldUpdateVertices))) {
                    continue;
                }

                int i = 5 * p;
                Vector3 currentPoint = currentAtom.point;
                Vector3 nextPoint = AtomsAsBase[p + 1].point;

                if (!specialRadii.TryGetValue(p, out float radius))
                    radius = baseRadius;

                vBase = radius * Vector3.Cross(nextPoint - camPos, nextPoint - currentPoint).normalized;
                vertices[i] = currentPoint + vBase;
                vertices[i + 1] = currentPoint - vBase;
                vertices[i + 2] = vertices[i] + nextPoint - currentPoint;
                vertices[i + 3] = vertices[i + 1] + nextPoint - currentPoint;
                if (p < atomCount - 2)
                    vertices[i + 4] = nextPoint;

                currentAtom.ShouldUpdateVertices = false;
            }

            mesh.vertices = vertices;
            mesh.RecalculateBounds();

            needsVerticesUpdate = false;
            forceFullVerticesUpdate = false;
        }

        private void UpdateColor() {
            Mesh mesh = GetComponent<MeshFilter>().sharedMesh;

            Color32[] colors = mesh.colors32;

            int atomCount = AtomsAsBase.Count;

            for (int p = 0; p < atomCount - 1; p++) {
                Atom currentAtom = AtomsAsBase[p];

                if (!currentAtom.ShouldDisplay || (!currentAtom.ShouldUpdateColor && (p == 0 || !AtomsAsBase[p - 1].ShouldUpdateColor))) {
                    continue;
                }

                int i = 5 * p;

                if (currentAtom.ShouldHighlight) {
                    colors[i] = currentAtom.highlightColor;
                    colors[i + 1] = currentAtom.highlightColor;
                    colors[i + 2] = currentAtom.highlightColor;
                    colors[i + 3] = currentAtom.highlightColor;
                    if (p < atomCount - 2)
                        colors[i + 4] = currentAtom.highlightColor;
                }
                else {
                    Atom nextAtom = AtomsAsBase[p + 1];
                    colors[i] = currentAtom.baseColor;
                    colors[i + 1] = currentAtom.baseColor;
                    colors[i + 2] = nextAtom.baseColor;
                    colors[i + 3] = nextAtom.baseColor;
                    if (p < atomCount - 2)
                        colors[i + 4] = nextAtom.baseColor;
                }

                currentAtom.ShouldUpdateColor = false;
            }

            mesh.colors32 = colors;
            needsColorUpdate = false;
        }

        private void UpdateTriangles() {
            int totalAtoms = AtomsAsBase.Count;

            CleanspecialRadii();

            List<int> trianglesL = new List<int>();
            int[] generator = { 0, 2, 1, 1, 2, 3, 2, 5, 4, 3, 4, 6 }; //vertices to create the triangles for a ribbon
            int[] bonusGenerator = { 2, 4, 5, 3, 6, 4 };

            bool previousWasSpecial = false;
            for (int i = 0; i < totalAtoms - 1; i++) {
                if (AtomsAsBase[i].ShouldDisplay) {
                    for (int j = 0; j < (i == totalAtoms - 2 ? 6 : 12); j++) {
                        trianglesL.Add(generator[j] + 5 * i);
                    }

                    if (specialRadii.ContainsKey(i)) {
                        if (!previousWasSpecial && i != 0 && AtomsAsBase[i - 1].ShouldDisplay) {
                            for (int j = 0; j < 6; j++) {
                                trianglesL.Add(bonusGenerator[j] + 5 * (i - 1));
                            }
                        }
                        if (i != totalAtoms - 2 && AtomsAsBase[i + 1].ShouldDisplay) {
                            for (int j = 0; j < 6; j++) {
                                trianglesL.Add(bonusGenerator[j] + 5 * (i));
                            }
                        }
                        previousWasSpecial = true;
                    }
                    else
                        previousWasSpecial = false;
                }
            }

            GetComponent<MeshFilter>().sharedMesh.triangles = trianglesL.ToArray();

            needsTriangleUpdate = false;
        }

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
    }

    public abstract class Atom {
        public Vector3 point;
        public Path path;
        public int indexInPath;

        private bool shouldUpdateVertices = false; //Should vertices be updated each frame?
        private bool shouldUpdateColor = false; //Should color be updated each frame?
        private bool shouldDisplay = true;
        private bool shouldHighlight = false;

        public Color32 baseColor = new Color32(255, 255, 255, 255);
        public Color32 highlightColor = new Color32(0, 255, 0, 255);

        public bool ShouldUpdateVertices {
            get => shouldUpdateVertices;
            set {
                shouldUpdateVertices = value;
                if (shouldUpdateVertices)
                    path.needsVerticesUpdate = true;
            }
        }

        public bool ShouldUpdateColor {
            get => shouldUpdateColor;
            set {
                shouldUpdateColor = value;
                if (shouldUpdateColor)
                    path.needsColorUpdate = true;
            }
        }

        public bool ShouldDisplay {
            get => shouldDisplay;
            set {
                if (value != shouldDisplay) {
                    path.needsTriangleUpdate = true;
                    shouldDisplay = value;
                    if (shouldDisplay) {
                        path.needsVerticesUpdate = true;
                        path.needsColorUpdate = true;
                    }
                }
            }
        }

        public bool ShouldHighlight {
            get {
                return shouldHighlight;
            }

            set {
                if (shouldHighlight != value) {
                    shouldHighlight = value;
                    shouldUpdateColor = true;
                    path.needsColorUpdate = true;
                }
            }
        }
    }

}