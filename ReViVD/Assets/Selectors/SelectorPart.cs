using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

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
                primitive.GetComponent<MeshRenderer>().material.color = _positive ? Color.white : Color.red;
            }
        }

        public void Show() {
            if (!isActiveAndEnabled)
                return;
            primitive.SetActive(true);
            primitive.GetComponent<Renderer>().material.color = SelectorManager.colors[(int)GetComponent<Selector>().Color];
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
                if (primitive != null && primitive.activeInHierarchy) {
                    primitive.GetComponent<Renderer>().material.color = SelectorManager.colors[(int)GetComponent<Selector>().Color];
                }
            }
        }
        protected abstract void UpdateManualModifications(); //Called every frame when the part is to be modified in creation mode

        private void FindRibbonsToCheck() {
            
            Visualization viz = Visualization.Instance;

            BoxCollider districtCollider = gameObject.AddComponent<BoxCollider>();
            districtCollider.size = Vector3.Scale(viz.districtSize, viz.transform.lossyScale);
            Vector3[] districtColliderDiags = new Vector3[4];
            for (int i = 0; i < 4; i++) {
                districtColliderDiags[i] = viz.districtSize;
                if (i != 0)
                    districtColliderDiags[i][i - 1] = -districtColliderDiags[i][i - 1];
                districtColliderDiags[i] = viz.transform.TransformVector(districtColliderDiags[i]);
            }
            districtCollider.center = Vector3.zero;
            Collider primitiveCollider = primitive.GetComponent<Collider>();

            int[] seedDistrict = viz.FindDistrictCoords(viz.transform.InverseTransformPoint(primitive.transform.position));

            Vector3 seedPos = viz.transform.TransformPoint(viz.getDistrictCenter(seedDistrict));
            Vector3 districtUnitTranslation = viz.transform.TransformVector(viz.districtSize);

            float get1DProjDistance_dc(Vector3 direction) {
                float dist = 0;
                foreach (Vector3 diag in districtColliderDiags) {
                    dist = Mathf.Max(dist, Mathf.Abs(Vector3.Dot(direction, diag)));
                }
                return dist;
            }

            CoordsEqualityComparer comparer = new CoordsEqualityComparer();
            HashSet<int[]> explored = new HashSet<int[]>(comparer);

            Tools.AddClockStop("Initialization of district checking 1");
            
            //PHASE 1: starting from the center, find a "border" district
            int[] start = new int[] { 0, 0, 0 };
            while (Physics.ComputePenetration(districtCollider, seedPos + Vector3.Scale(districtUnitTranslation, new Vector3(start[0], start[1], start[2])), viz.transform.rotation,
                                                   primitiveCollider, primitive.transform.position, primitive.transform.rotation, out _, out _)) {
                start[0] += 1;
            }
            start[0] -= 1;

            Tools.AddClockStop("End phase 1");
            
            //PHASE 2: starting from the border district found, create a "shell" of border districts around the (convex) primitive
            bool addToShell(int[] c) { //Checks if c is part of the shell and wasn't explored before and returns true if it is
                if (!explored.Add(c)) {
                    return false;
                }

                //Check whether or not the district touches the primitive
                if (Physics.ComputePenetration(districtCollider, seedPos + Vector3.Scale(districtUnitTranslation, new Vector3(c[0], c[1], c[2])), viz.transform.rotation,
                                                   primitiveCollider, primitive.transform.position, primitive.transform.rotation, out Vector3 exitDir, out float exitDist)) {

                    int[] true_c = new int[] { c[0] + seedDistrict[0], c[1] + seedDistrict[1], c[2] + seedDistrict[2] }; //True coordinates of the district in the visualization dictionary

                    if (get1DProjDistance_dc(exitDir) > exitDist) { //Border district: add ribbons to ribbonsToCheck and keep spreading
                        if (viz.districts.TryGetValue(true_c, out Visualization.District d)) {
                            if (viz.debugMode)
                                viz.districtsToHighlight[1].Add(true_c); //DEBUG

                            foreach (Atom a in d.atoms) {
                                if (a.ShouldDisplay) {
                                    ribbonsToCheck.Add(a);
                                }
                            }
                        }

                        return true;
                    }
                    else { //Inside district: add ribbons to touchedRibbons and stop the spreading there
                        if (viz.districts.TryGetValue(true_c, out Visualization.District d)) {

                            foreach (Atom a in d.atoms) {
                                if (a.ShouldDisplay) {
                                    touchedRibbons.Add(a);
                                }
                            }
                        }

                        return false;
                    }
                }
                else {
                    return false;
                }
            }

            HashSet<int[]> set1 = new HashSet<int[]>(comparer);
            HashSet<int[]> set2 = new HashSet<int[]>(comparer);
            set1.Add(start);
            int cycle = 0;
            while (set1.Count != 0) {
                foreach (int[] c in set1) {
                    if (addToShell(c)) {
                        for (int i = 0; i < 3; i++) {
                            for (int j = -1; j <= 1; j += 2) {
                                int[] next_c = (int[])c.Clone();
                                next_c[i] += j;
                                set2.Add(next_c);
                            }
                        }
                    }
                }

                Tools.AddSubClockStop("End of cycle 2." + cycle.ToString() + "; " + set1.Count.ToString() + " districts treated");

                HashSet<int[]> temp = set1;
                set1.Clear();
                set1 = set2;
                set2 = temp;

                cycle++;
            }

            Tools.AddClockStop("End phase 2");

            //PHASE 3: starting from the center, flood the created shell
            bool addToInside(int[] c) { //Checks if c was explored before and returns true if it wasn't
                if (!explored.Add(c)) {
                    return false;
                }

                int[] true_c = new int[] { c[0] + seedDistrict[0], c[1] + seedDistrict[1], c[2] + seedDistrict[2] }; //True coordinates of the district in the visualization dictionary

                if (viz.districts.TryGetValue(true_c, out Visualization.District d)) {
                    if (viz.debugMode)
                        viz.districtsToHighlight[2].Add(true_c); //DEBUG

                    foreach (Atom a in d.atoms) {
                        if (a.ShouldDisplay) {
                            TouchedRibbons.Add(a);
                        }
                    }
                }

                return true;
            }

            set1.Clear();
            set2.Clear();
            set1.Add(new int[] { 0, 0, 0 });
            cycle = 0;
            while (set1.Count != 0) {
                foreach (int[] c in set1) {
                    if (addToInside(c)) {
                        for (int i = 0; i < 3; i++) {
                            for (int j = -1; j <= 1; j += 2) {
                                if (c[i] == 0 || c[i] > 0 == j > 0) { //Only flood towards the exterior
                                    int[] next_c = (int[])c.Clone();
                                    next_c[i] += j;
                                    set2.Add(next_c);
                                }
                            }
                        }
                    }
                }

                Tools.AddSubClockStop("End of cycle 3." + cycle.ToString() + "; " + set1.Count.ToString() + " districts treated");

                HashSet<int[]> temp = set1;
                set1.Clear();
                set1 = set2;
                set2 = temp;

                cycle++;
            }

            Tools.AddClockStop("End phase 3");

            ribbonsToCheck.ExceptWith(touchedRibbons);

            Tools.AddClockStop("Removed obvious from toCheck");

            Destroy(districtCollider);
        }

        protected abstract void ParseRibbonsToCheck();

        protected abstract void CreatePrimitive();

        protected abstract void UpdatePrimitive();

        protected virtual void Awake() {
            CreatePrimitive();
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