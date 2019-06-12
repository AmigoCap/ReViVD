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

        public bool needsColorUpdate = false; //Update the color of the vertices

        private Mesh mesh;

        protected virtual void Awake() {
            MeshFilter filter = gameObject.AddComponent<MeshFilter>();
            mesh = new Mesh();
            filter.sharedMesh = mesh;
            MeshRenderer renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = Visualization.Instance.material;
            needsMeshUpdate = true;
        }

        public void UpdatePath() {
            if (needsMeshUpdate)
                UpdateMesh();
            if (needsColorUpdate)
                UpdateColor();
            if (needsTriangleUpdate)
                UpdateTriangles();
        }

        private void UpdateMesh() {
            mesh.Clear();

            int AtomCount = AtomsAsBase.Count;
            //Debug.Log(AtomCount * 5 - 6);

            Vector3[] vertices = new Vector3[AtomCount * 5 - 6];
            for (int p = 0; p < AtomCount - 1; p++) {
                int i = 5 * p;
                ref Vector3 currentPoint = ref AtomsAsBase[p].point;
                ref Vector3 nextPoint = ref AtomsAsBase[p + 1].point;

                vertices[i] = currentPoint;
                vertices[i + 1] = currentPoint;
                vertices[i + 2] = nextPoint;
                vertices[i + 3] = nextPoint;
                if (p < AtomCount - 2)
                    vertices[i + 4] = nextPoint;
            }
            
            mesh.vertices = vertices;
            mesh.colors32 = new Color32[vertices.Length];

            CleanspecialRadii();

            List<Vector4> uvs = new List<Vector4>(); //Sneakily putting extra vertex info in the uv textures field
            for (int p = 0; p < AtomCount - 1; p++) {
                Vector3 v = AtomsAsBase[p + 1].point - AtomsAsBase[p].point;
                if (!specialRadii.TryGetValue(p, out float radius))
                    radius = baseRadius;
                for (int i = 0; i < (p == AtomCount - 2 ? 4 : 5); i++) {
                    float extra = 0;
                    if (i == 0 || i == 2)
                        extra = -radius;
                    else if (i == 1 || i == 3)
                        extra = radius;
                    uvs.Add(new Vector4(v.x, v.y, v.z, extra));
                }
            }
            mesh.SetUVs(0, uvs);

            needsMeshUpdate = false;
            needsTriangleUpdate = true;
            needsColorUpdate = true;
        }

        private void UpdateColor() {
            Color32[] colors = mesh.colors32;

            int atomCount = AtomsAsBase.Count;

            for (int p = 0; p < atomCount - 1; p++) {
                Atom currentAtom = AtomsAsBase[p];

                if (!currentAtom.ShouldDisplay || (!currentAtom.ShouldUpdateColor && (p == 0 || !AtomsAsBase[p - 1].ShouldUpdateColor))) {
                    continue;
                }

                int i = 5 * p;

                if (currentAtom.ShouldHighlight) {
                    Color32 color = currentAtom.HighlightColor;
                    colors[i] = color;
                    colors[i + 1] = color;
                    colors[i + 2] = color;
                    colors[i + 3] = color;
                    if (p < atomCount - 2)
                        colors[i + 4] = color;
                }
                else {
                    Atom nextAtom = AtomsAsBase[p + 1];
                    colors[i] = currentAtom.BaseColor;
                    colors[i + 1] = currentAtom.BaseColor;
                    colors[i + 2] = nextAtom.BaseColor;
                    colors[i + 3] = nextAtom.BaseColor;
                    if (p < atomCount - 2)
                        colors[i + 4] = nextAtom.BaseColor;
                }

                currentAtom.ShouldUpdateColor = false;
            }

            mesh.colors32 = colors;
            needsColorUpdate = false;
        }

        private void UpdateTriangles() {
            int totalAtoms = AtomsAsBase.Count;

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

            mesh.triangles = trianglesL.ToArray();

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

        bool BitArray_Or(BitArray a) {
            foreach (bool b in a)
                if (b)
                    return true;
            return false;
        }

        private bool shouldDisplay = true;
        private bool shouldUpdateColor = false;
        private BitArray shouldHighlight_checked = new BitArray(SelectorManager.colors.Length, false);
        private BitArray shouldHighlight_selected = new BitArray(SelectorManager.colors.Length, false);
        private bool shouldHighlight_debug = false;

        private Color32 baseColor = new Color32(255, 255, 255, 255);
        private Color32 highlightColor = new Color32(0, 255, 0, 255);

        public bool ShouldUpdateColor {
            get => shouldUpdateColor;
            set {
                shouldUpdateColor = value;
                if (shouldUpdateColor)
                    path.needsColorUpdate = true;
            }
        }

        public Color32 BaseColor {
            get => baseColor;
            set {
                if (!baseColor.Equals(value)) {
                    baseColor = value;
                    shouldUpdateColor = true;
                    path.needsColorUpdate = true;
                }
            }
        }

        public Color32 HighlightColor {
            get {
                for (int i = 0; i < SelectorManager.colors.Length; i++) {
                    if (shouldHighlight_selected[i])
                        return SelectorManager.colors[i];
                }
                for (int i = 0; i < SelectorManager.colors.Length; i++) {
                    if (shouldHighlight_checked[i])
                        return SelectorManager.colors_dark[i];
                }
                return highlightColor;

            }
            set {
                if (!highlightColor.Equals(value)) {
                    highlightColor = value;
                    if (ShouldHighlight && !BitArray_Or(shouldHighlight_checked) && !BitArray_Or(shouldHighlight_selected)) {
                        shouldUpdateColor = true;
                        path.needsColorUpdate = true;
                    }
                }
            }
        }

        public bool ShouldDisplay {
            get => shouldDisplay;
            set {
                if (value != shouldDisplay) {
                    path.needsTriangleUpdate = true;
                    shouldDisplay = value;
                    if (shouldDisplay) {
                        path.needsColorUpdate = true;
                    }
                }
            }
        }

        public bool ShouldHighlight {
            get {
                return BitArray_Or(shouldHighlight_checked) || BitArray_Or(shouldHighlight_selected) || shouldHighlight_debug;
            }
        }

        public void ShouldHighlightBecauseChecked(int colorGroup, bool value = true) {
            bool wasHighlighted = ShouldHighlight;
            shouldHighlight_checked[colorGroup] = value;
            if ((!wasHighlighted && value) || (wasHighlighted && !ShouldHighlight)) {
                shouldUpdateColor = true;
                path.needsColorUpdate = true;
            }
        }

        public void ShouldHighlightBecauseSelected(int colorGroup, bool value = true) {
            bool wasHighlighted = ShouldHighlight;
            shouldHighlight_selected[colorGroup] = value;
            if ((!wasHighlighted && value) || (wasHighlighted && !ShouldHighlight)) {
                shouldUpdateColor = true;
                path.needsColorUpdate = true;
            }
        }

        public bool ShouldHighlightBecauseDebug {
            get => shouldHighlight_debug;
            set {
                bool wasHighlighted = ShouldHighlight;
                shouldHighlight_debug = value;
                if ((!wasHighlighted && value) || (wasHighlighted && !ShouldHighlight)) {
                    shouldUpdateColor = true;
                    path.needsColorUpdate = true;
                }
            }
        }
    }

}