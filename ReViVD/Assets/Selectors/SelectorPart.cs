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
            if (GetComponent<Selector>().Persistent)
                DetachFromHand();
            else
                AttachToHand();
        }

        public void Hide() {
            if (primitive != null)
                primitive.SetActive(false);
        }

        public Transform PrimitiveTransform {
            get => primitive?.transform;
        }

        public void FindTouchedRibbons() {
            UpdatePrimitive();

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

            ParseRibbonsToCheck();
        }

        protected GameObject primitive;

        protected abstract void CreatePrimitive();

        protected abstract void AttachToHand();

        private void DetachFromHand() {
            primitive.transform.parent = this.transform;
        }

        protected abstract void UpdatePrimitive(); //Populates variables that define the geometry of the primitive used in collision detection
        
        protected abstract void FindDistrictsToCheck();

        protected abstract void ParseRibbonsToCheck();

        protected virtual void Awake() {
            CreatePrimitive();
            Destroy(primitive.GetComponent<Collider>());
        }

        protected virtual void OnEnable() {
            Selector s = GetComponent<Selector>();

            if (!s.Persistent)
                AttachToHand();
            else
                DetachFromHand();

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