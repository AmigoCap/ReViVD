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
            districtCollider.center = Vector3.zero;
            Collider primitiveCollider = primitive.GetComponent<Collider>();

            int[] seedDistrict = viz.FindDistrictCoords(viz.transform.InverseTransformPoint(primitive.transform.position));

            Vector3 seedPos = viz.transform.TransformPoint(viz.getDistrictCenter(seedDistrict));
            Vector3 districtUnitTranslation = viz.transform.TransformVector(viz.districtSize);

            Dictionary<int[], bool> explored = new Dictionary<int[], bool>(new CoordsEqualityComparer());

            bool floodFrom(int[] c) {
                if (explored.TryGetValue(c, out bool inside)) {
                    return inside;
                }

                if (Physics.ComputePenetration(districtCollider, seedPos + Vector3.Scale(districtUnitTranslation, new Vector3(c[0], c[1], c[2])), viz.transform.rotation,
                                                   primitiveCollider, primitive.transform.position, primitive.transform.rotation, out _, out _)) {

                    explored.Add(c, true);

                    bool isObvious = true;

                    for (int i = 0; i < 3; i++) {
                        for (int j = -1; j < 2; j += 2) {
                            if (c[i] == 0 || c[i] > 0 == j > 0) {
                                int[] next_c = (int[])c.Clone();
                                next_c[i] += j;
                                if (!floodFrom(next_c))
                                    isObvious = false;
                            }
                        }

                    }

                    int[] true_c = new int[] { c[0] + seedDistrict[0], c[1] + seedDistrict[1], c[2] + seedDistrict[2] };
                    if (Visualization.Instance.districts.TryGetValue(true_c, out Visualization.District d)) {
                        if (isObvious) {
                            foreach (Atom a in d.atoms_segment) {
                                if (a.ShouldDisplay) {
                                    touchedRibbons.Add(a);
                                }
                            }
                        }
                        else {
                            foreach (Atom a in d.atoms_segment) {
                                if (a.ShouldDisplay) {
                                    ribbonsToCheck.Add(a);
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

            floodFrom(new int[] { 0, 0, 0 });

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