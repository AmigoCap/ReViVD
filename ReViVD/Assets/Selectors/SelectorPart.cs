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

        public abstract void UpdatePrimitive();

        public abstract void FindDistrictsToCheck();

        public abstract void FindTouchedRibbons();

        protected virtual void OnEnable() {
            CreatePrimitive();
        }

        protected virtual void OnDisable() {
            Destroy(primitive);
        }
    }

}