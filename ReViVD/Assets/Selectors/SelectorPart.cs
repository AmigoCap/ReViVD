using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Revivd { 

    public abstract class SelectorPart : MonoBehaviour {
        protected HashSet<Atom> ribbonsToCheck = new HashSet<Atom>();
        public HashSet<Atom> RibbonsToCheck { get => ribbonsToCheck; }
        protected HashSet<Atom> touchedRibbons = new HashSet<Atom>();
        public HashSet<Atom> TouchedRibbons { get => touchedRibbons; }

        [SerializeField]
        private bool _positive = true;
        public bool Positive {
            get => _positive;
            set {
                _positive = value;
            }
        }

        public void Show() {
            if (!isActiveAndEnabled)
                return;
            primitive.SetActive(true);
            Color32 color = SelectorManager.colors[(int)GetComponent<Selector>().Color];
            color.a = SelectorManager.Instance.selectorTransparency;
            primitive.GetComponent<Renderer>().material.color = color;
            if (GetComponent<Selector>().Persistent) {
                primitive.transform.SetParent(this.transform, false);
            }
            else
                primitive.transform.SetParent(SteamVR_ControllerManager.Instance.right.transform, false);
            UpdatePrimitive();
        }

        public void Hide() {
            if (primitive != null)
                primitive.SetActive(false);
        }

        public Transform PrimitiveTransform {
            get => primitive?.transform;
        }
        
        public void FindTouchedRibbons() {
            touchedRibbons.Clear();
            ribbonsToCheck.Clear();

            Tools.AddClockStop("Cleared old ribbons");

            FindRibbonsToCheck();

            Tools.AddClockStop("Found ribbons to check");

            ParseRibbonsToCheck();
        }

        protected GameObject primitive;

        private bool _shouldPollManualModifications;
        public bool ShouldPollManualModifications {
            get => _shouldPollManualModifications;
            set {
                _shouldPollManualModifications = value;
                if (!_shouldPollManualModifications && primitive != null && primitive.activeInHierarchy) {
                    Color32 color = SelectorManager.colors[(int)GetComponent<Selector>().Color];
                    color.a = color.a = SelectorManager.Instance.selectorTransparency;
                    primitive.GetComponent<Renderer>().material.color = color;
                }
            }
        }
        protected abstract void UpdateManualModifications(); //Called every frame when the part is to be modified in creation mode

        private void FindRibbonsToCheck() {

            Visualization viz = Visualization.Instance;

            BoxCollider districtCollider = gameObject.AddComponent<BoxCollider>();
            districtCollider.size = Vector3.Scale(viz.districtSize, viz.transform.lossyScale);

            districtCollider.center = Vector3.zero;
            Collider primitiveCollider = primitive.GetComponent<Collider>();

            int[] seedDistrict = viz.FindDistrictCoords(viz.transform.InverseTransformPoint(primitive.transform.position));
            Vector3 trueDistrictSize = viz.transform.TransformVector(viz.districtSize);
            float districtDiagLength = trueDistrictSize.magnitude;
            float districtMinSideLength = Mathf.Abs(trueDistrictSize[0]);
            districtMinSideLength = Mathf.Min(districtMinSideLength, Mathf.Abs(trueDistrictSize[1]));
            districtMinSideLength = Mathf.Min(districtMinSideLength, Mathf.Abs(trueDistrictSize[2]));

            Vector3 seedPos = viz.transform.TransformPoint(viz.getDistrictCenter(seedDistrict));
            Vector3 districtUnitTranslation = viz.transform.TransformVector(viz.districtSize);

            CoordsEqualityComparer comparer = new CoordsEqualityComparer();
            Dictionary<int[], byte> districtMap = new Dictionary<int[], byte>(comparer);
            const byte outside = 0;
            const byte border_done = 1;
            const byte border = 2;
            const byte border_or_inside = 3;
            const byte inside_done = 4;
            const byte inside = 5;

            byte getDepth(int[] coords) {
                if (!districtMap.TryGetValue(coords, out byte depth)) {
                    if (Physics.ComputePenetration(districtCollider, seedPos + Vector3.Scale(districtUnitTranslation, new Vector3(coords[0], coords[1], coords[2])), viz.transform.rotation,
                                                   primitiveCollider, primitive.transform.position, primitive.transform.rotation, out _, out float exitDist)) {
                        if (exitDist > districtDiagLength)
                            depth = inside;
                        else if (exitDist < districtMinSideLength)
                            depth = border;
                        else
                            depth = border_or_inside;
                    }
                    else {
                        depth = outside;
                    }
                    districtMap.Add((int[])coords.Clone(), depth);
                }
                return depth;
            }

            Tools.AddClockStop("Initialized district checking");

            //PHASE 1: starting from the center, find a "border" district
            int[] start = new int[] { 0, 0, 0 };
            while (getDepth(start) != outside) {
                start[0] += 1;
            }
            start[0] -= 1;
            districtMap[(int[])start.Clone()] = border;

            Tools.AddClockStop("End of phase 1");

            //PHASE 2: starting from the border district found, create a "shell" of border districts around the (convex) primitive

            HashSet<int[]> districtsToSpreadFrom = new HashSet<int[]>(comparer);
            HashSet<int[]> nextDistrictsToSpreadFrom = new HashSet<int[]>(comparer);
            districtsToSpreadFrom.Add(start);
            int cycle = 0;
            while (districtsToSpreadFrom.Count > 0) {
                foreach (int[] c in districtsToSpreadFrom) {
                    byte depth = getDepth(c);

                    if (depth == inside || depth == border_done) {
                        continue;
                    }

                    int[][] neighbours = new int[26][];
                    byte[] neighboursDepth = new byte[26];
                    int index = 0;
                    for (int i = -1; i <= 1; i++) {
                        for (int j = -1; j <= 1; j++) {
                            for (int k = -1; k <= 1; k++) {
                                if (i == 0 && j == 0 && k == 0)
                                    continue;
                                neighbours[index] = (int[])c.Clone();
                                neighbours[index][0] += i;
                                neighbours[index][1] += j;
                                neighbours[index][2] += k;
                                neighboursDepth[index] = getDepth(neighbours[index]);
                                index++;
                            }
                        }
                    }

                    if (depth == border_or_inside) {
                        for (int i = 0; i < 26; i++) {
                            if (neighboursDepth[i] == outside)
                                depth = border;
                        }
                        if (depth == border_or_inside)
                            depth = inside;
                    }

                    if (depth == border) {
                        for (int i = 0; i < 26; i++) {
                            if (neighboursDepth[i] == border || neighboursDepth[i] == border_or_inside)
                                nextDistrictsToSpreadFrom.Add(neighbours[i]);
                        }

                        int[] true_c = new int[] { c[0] + seedDistrict[0], c[1] + seedDistrict[1], c[2] + seedDistrict[2] };
                        if (viz.districts.TryGetValue(true_c, out Visualization.District d)) {
                            if (viz.debugMode)
                                viz.districtsToHighlight[0].Add(true_c); //DEBUG

                            foreach (Atom a in d.atoms) {
                                if (a.ShouldDisplay) {
                                    ribbonsToCheck.Add(a);
                                }
                            }
                        }

                        depth = border_done;
                    }

                    districtMap[c] = depth;
                }

                Tools.AddSubClockStop("End of cycle 2." + cycle.ToString() + "; " + districtsToSpreadFrom.Count.ToString() + " districts treated");

                HashSet<int[]> temp = districtsToSpreadFrom;
                districtsToSpreadFrom.Clear();
                districtsToSpreadFrom = nextDistrictsToSpreadFrom;
                nextDistrictsToSpreadFrom = temp;

                cycle++;
            }

            Tools.AddClockStop("End of phase 2");

            //PHASE 3: starting from the center, flood the created shell

            districtsToSpreadFrom.Clear();
            nextDistrictsToSpreadFrom.Clear();
            int[] center = new int[] { 0, 0, 0 };
            if (districtMap.ContainsKey(center) && districtMap[center] != inside) {
                Tools.AddClockStop("Skipping phase 3: center district is not an inside district");
            }
            else {
                districtsToSpreadFrom.Add(center);
            }
            cycle = 0;
            while (districtsToSpreadFrom.Count > 0) {
                foreach (int[] c in districtsToSpreadFrom) {
                    for (int i = 0; i < 3; i++) {
                        for (int j = -1; j <= 1; j += 2) {
                            if (c[i] == 0 || c[i] > 0 == j > 0) { //Only flood towards the exterior (the selector part is convex)
                                int[] neighbour = (int[])c.Clone();
                                neighbour[i] += j;
                                if (!districtMap.TryGetValue(neighbour, out byte neighbourDepth)) {
                                    neighbourDepth = inside;
                                    districtMap[neighbour] = neighbourDepth;
                                }
                                if (neighbourDepth == inside)
                                    nextDistrictsToSpreadFrom.Add(neighbour);
                            }
                        }
                    }

                    int[] true_c = new int[] { c[0] + seedDistrict[0], c[1] + seedDistrict[1], c[2] + seedDistrict[2] }; //True coordinates of the district in the visualization dictionary

                    if (viz.districts.TryGetValue(true_c, out Visualization.District d)) {
                        if (viz.debugMode)
                            viz.districtsToHighlight[1].Add(true_c); //DEBUG

                        foreach (Atom a in d.atoms) {
                            if (a.ShouldDisplay) {
                                TouchedRibbons.Add(a);
                            }
                        }
                    }

                    districtMap[c] = inside_done;
                }

                Tools.AddSubClockStop("End of cycle 3." + cycle.ToString() + "; " + districtsToSpreadFrom.Count.ToString() + " districts treated");

                HashSet<int[]> temp = districtsToSpreadFrom;
                districtsToSpreadFrom.Clear();
                districtsToSpreadFrom = nextDistrictsToSpreadFrom;
                nextDistrictsToSpreadFrom = temp;

                cycle++;
            }

            Tools.AddClockStop("End of phase 3");

            ribbonsToCheck.ExceptWith(touchedRibbons);

            Tools.AddClockStop("Removed obvious touchedRibbons from ribbonsToCheck");

            Destroy(districtCollider);
        }

        protected abstract void ParseRibbonsToCheck();

        protected abstract void CreatePrimitive();

        protected abstract void UpdatePrimitive();

        protected virtual void Awake() {
            CreatePrimitive();
            primitive.GetComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/Selector");
            UpdatePrimitive();
        }

        private static readonly float colorPulseFrequency = 10;
        private static readonly float colorPulseAmplitude = 0.15f;

        protected virtual void Update() {
            if (primitive == null || !primitive.activeInHierarchy)
                return;

            Selector s = GetComponent<Selector>();
            if (s.isActiveAndEnabled && s.Shown) {
                if (ShouldPollManualModifications) {
                    UpdateManualModifications();
                    primitive.GetComponent<Renderer>().material.color = SelectorManager.colors[(int)s.Color] * (1 - colorPulseAmplitude * Mathf.Sin(colorPulseFrequency * Time.time));
                }

                UpdatePrimitive();
            }
        }

        protected virtual void OnEnable() {
            Selector s = GetComponent<Selector>();

            if (s.Persistent) {
                primitive.transform.SetParent(this.transform, false);
            }
            else
                primitive.transform.SetParent(SteamVR_ControllerManager.Instance.right.transform, false);
            UpdatePrimitive();

            if (s.Shown && s.isActiveAndEnabled)
                Show();
            else
                Hide();
        }

        protected virtual void OnDisable() {
            Hide();
        }

        protected virtual void OnDestroy() {
            Destroy(primitive);
        }
    }

}