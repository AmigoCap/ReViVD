using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Revivd { 

    public abstract class SelectorPart : MonoBehaviour {
        protected HashSet<int[]> checkedDistricts = new HashSet<int[]>(new CoordsEqualityComparer());
        public HashSet<int[]> CheckedDistricts { get => checkedDistricts; }
        protected HashSet<Atom> checkedRibbons = new HashSet<Atom>();
        public HashSet<Atom> CheckedRibbons { get => checkedRibbons; }
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
            checkedDistricts.Clear();
            FindDistrictsToCheck();

            checkedRibbons.Clear();
            foreach (int[] c in checkedDistricts) {
                if (Visualization.Instance.districts.TryGetValue(c, out Visualization.District d)) {
                    foreach (Atom a in d.atoms_segment) {
                        if (a.ShouldDisplay) {
                            checkedRibbons.Add(a);
                        }
                    }
                }
            }

            touchedRibbons.Clear();
            ParseRibbonsToCheck();
        }

        protected GameObject primitive;

        public bool ShouldPollManualModifications = false;
        protected abstract void UpdateManualModifications(); //Called every frame when the part is to be modified in creation mode

        protected abstract void ParseRibbonsToCheck();

        protected abstract void CreatePrimitive();

        protected abstract void UpdatePrimitive();
        
        private void FindDistrictsToCheck() {
            Visualization viz = Visualization.Instance;

            BoxCollider districtCollider = gameObject.AddComponent<BoxCollider>();
            districtCollider.size = Vector3.Scale(viz.districtSize, viz.transform.lossyScale);
            districtCollider.center = Vector3.zero;
            Collider primitiveCollider = primitive.GetComponent<Collider>();

            int[] seedDistrict = viz.FindDistrictCoords(viz.transform.InverseTransformPoint(primitive.transform.position));


            Vector3 seedPos = viz.transform.TransformPoint(viz.getDistrictCenter(seedDistrict));
            Vector3 districtUnitTranslation = viz.transform.TransformVector(viz.districtSize);
            CoordsEqualityComparer comparer = new CoordsEqualityComparer();
            HashSet<int[]> set1 = new HashSet<int[]>(comparer);
            HashSet<int[]> set2 = new HashSet<int[]>(comparer);

            set1.Add(new int[] { 0, 0, 0 });

            bool foundAll = false;
            while (!foundAll) {
                foundAll = true;
                
                foreach (int[] c in set1) {
                    if (Physics.ComputePenetration(districtCollider, seedPos + Vector3.Scale(districtUnitTranslation, new Vector3(c[0], c[1], c[2])), viz.transform.rotation,
                                                   primitiveCollider, primitive.transform.position, primitive.transform.rotation, out _, out _)) {

                        foundAll = false;
                        checkedDistricts.Add(new int[] { seedDistrict[0] + c[0], seedDistrict[1] + c[1], seedDistrict[2] + c[2] });
                        Visualization.Instance.districtsToHighlight[0] = checkedDistricts;

                        for (int i = 0; i < 3; i++) {
                            for (int j = -1; j < 2; j += 2) {
                                if (c[i] == 0 || c[i] > 0 == j > 0) {
                                    int[] d2 = (int[])c.Clone();
                                    d2[i] += j;
                                    set2.Add(d2);
                                }
                            }
                        }
                    }
                }

                HashSet<int[]> temp = set1;
                set1 = set2;
                set2 = temp;
                set2.Clear();
            }

            Destroy(districtCollider);
        }

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