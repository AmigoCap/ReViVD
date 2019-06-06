using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Jobs;
using Unity.Collections;

namespace Revivd { 

    public abstract class SelectorPart : MonoBehaviour {
        protected HashSet<Atom> ribbonsToCheck = new HashSet<Atom>();
        public HashSet<Atom> RibbonsToCheck { get => ribbonsToCheck; }
        protected List<Atom> touchedRibbons = new List<Atom>();
        public List<Atom> TouchedRibbons { get => touchedRibbons; }

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
                this.transform.position = SteamVR_ControllerManager.Instance.right.transform.position;
                this.transform.rotation = SteamVR_ControllerManager.Instance.right.transform.rotation;
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

            FindRibbonsToCheck();

            ParseRibbonsToCheck();
        }

        protected GameObject primitive;

        public bool ShouldPollManualModifications = false;
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

            CoordsEqualityComparer c = new CoordsEqualityComparer();
            Dictionary<int[], bool> explored = new Dictionary<int[], bool>(c);

            //PHASE 1: starting from the center, find a "border" district
            int[] start = new int[] { 0, 0, 0 };
            while (Physics.ComputePenetration(districtCollider, seedPos + Vector3.Scale(districtUnitTranslation, new Vector3(start[0], start[1], start[2])), viz.transform.rotation,
                                                   primitiveCollider, primitive.transform.position, primitive.transform.rotation, out _, out _)) {
                start[0] += 1;
            }
            start[0] -= 1;

            int N = 0;

            //PHASE 2: starting from the border district found, recursively create a "shell" of border districts around the (convex) primitive
            bool floodShell(int[] c) {
                if (explored.TryGetValue(c, out bool intersects)) {
                    return intersects;
                }

                N++;

                //Check whether or not the district touches the primitive
                if (Physics.ComputePenetration(districtCollider, seedPos + Vector3.Scale(districtUnitTranslation, new Vector3(c[0], c[1], c[2])), viz.transform.rotation,
                                                   primitiveCollider, primitive.transform.position, primitive.transform.rotation, out Vector3 exitDir, out float exitDist)) {

                    explored.Add(c, true);

                    int[] true_c = new int[] { c[0] + seedDistrict[0], c[1] + seedDistrict[1], c[2] + seedDistrict[2] }; //True coordinates of the district in the visualization dictionary

                    if (get1DProjDistance_dc(exitDir) > exitDist) { //Border district: add ribbons to ribbonsToCheck and keep spreading
                        if (viz.districts.TryGetValue(true_c, out Visualization.District d)) {
                            viz.districtsToHighlight[1].Add(true_c);

                            foreach (Atom a in d.atoms_segment) {
                                if (a.ShouldDisplay) {
                                    ribbonsToCheck.Add(a);
                                }
                            }
                        }

                        for (int i = 0; i < 3; i++) {
                            for (int j = -1; j <= 1; j+=2) {
                                int[] next_c = (int[])c.Clone();
                                next_c[i] += j;
                                floodShell(next_c);
                            }
                        }
                    }
                    else { //Inside district: add ribbons to touchedRibbons and stop the spreading there
                        if (viz.districts.TryGetValue(true_c, out Visualization.District d)) {
                            
                            foreach (Atom a in d.atoms_segment) {
                                if (a.ShouldDisplay) {
                                    TouchedRibbons.Add(a);
                                }
                            }
                        }
                    }

                    return true;
                }
                else {
                    explored.Add(c, false);
                    return false;
                }
            }

            floodShell(start);

            Debug.Log(N);

            //PHASE 3: starting from the center, flood the created shell recursively
            bool floodInside(int[] c) {
                if (explored.TryGetValue(c, out bool intersects)) {
                    return intersects;
                }

                explored.Add(c, true);

                int[] true_c = new int[] { c[0] + seedDistrict[0], c[1] + seedDistrict[1], c[2] + seedDistrict[2] }; //True coordinates of the district in the visualization dictionary

                if (viz.districts.TryGetValue(true_c, out Visualization.District d)) {
                    foreach (Atom a in d.atoms_segment) {
                        if (a.ShouldDisplay) {
                            TouchedRibbons.Add(a);
                        }
                    }
                }

                for (int i = 0; i < 3; i++) {
                    for (int j = -1; j <= 1; j+=2) {
                        if (c[i] == 0 || c[i] > 0 == j > 0) { //Only flood towards the exterior
                            int[] next_c = (int[])c.Clone();
                            next_c[i] += j;
                            if (!floodInside(next_c))
                                Debug.LogWarning("Flooding algorithm from inside of primitive managed to contact its exterior, this should not happen");
                        }
                    }
                }

                return true;
            }

            floodInside(new int[] { 0, 0, 0 });

            ribbonsToCheck.ExceptWith(touchedRibbons);

            Destroy(districtCollider);
        }


        protected struct IParseRibbonsJob : IJobParallelFor {
            [ReadOnly, NativeDisableParallelForRestriction]
            public NativeArray<Vector3> points;

            public void Execute(int i) {

            }
        }

        protected abstract void ParseRibbonsToCheck();

        protected abstract void CreatePrimitive();

        protected abstract void UpdatePrimitive();

        protected virtual void Awake() {
            CreatePrimitive();
            UpdatePrimitive();
        }

        protected virtual void Update() {
            if (primitive != null && !primitive.activeInHierarchy)
                return;

            if (ShouldPollManualModifications)
                UpdateManualModifications();

            UpdatePrimitive();
        }

        protected virtual void OnEnable() {
            Selector s = GetComponent<Selector>();

            if (s.Persistent) {
                primitive.transform.SetParent(this.transform, false);
                this.transform.position = SteamVR_ControllerManager.Instance.right.transform.position;
                this.transform.rotation = SteamVR_ControllerManager.Instance.right.transform.rotation;
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