using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Revivd { 

    public abstract class SelectorPart : MonoBehaviour {
        public HashSet<int[]> districtsToCheck = new HashSet<int[]>(new CoordsEqualityComparer());
        public HashSet<Atom> ribbonsToCheck = new HashSet<Atom>();
        public HashSet<Atom> touchedRibbons = new HashSet<Atom>();

        [SerializeField]
        private bool _positive = true;
        public bool Positive {
            get => _positive;
            set {
                _positive = value;
                primitive.GetComponent<MeshRenderer>().material.color = _positive ? Color.white : Color.red;
            }
        }

        protected GameObject primitive;

        protected abstract void CreatePrimitive();

        protected abstract void AttachToHand();

        private void DetachFromHand() {
            primitive.transform.parent = this.transform;
        }

        public void Show() {
            if (!isActiveAndEnabled)
                return;
            primitive.SetActive(true);
            primitive.GetComponent<Renderer>().material.color = SelectorManager.colors[(int)GetComponent<Selector>().Color];
            if (!GetComponent<Selector>().Persistent)
                AttachToHand();
            else
                DetachFromHand();
        }

        public void Hide() {
            primitive.SetActive(false);
        }

        public abstract void UpdatePrimitive();
        
        public abstract void FindDistrictsToCheck();

        public abstract void FindTouchedRibbons();

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