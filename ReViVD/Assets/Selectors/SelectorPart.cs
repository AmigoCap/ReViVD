using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Revivd { 

    public abstract class SelectorPart : MonoBehaviour {
        public HashSet<int[]> districtsToCheck = new HashSet<int[]>(new CoordsEqualityComparer());
        public HashSet<Atom> ribbonsToCheck = new HashSet<Atom>();

        public bool positive = true;

        protected GameObject primitive;

        protected abstract void CreatePrimitive();

        public abstract void UpdatePrimitive();

        public abstract void FindDistrictsToCheck();

        public abstract void AddToSelectedRibbons();

        protected virtual void OnEnable() {
            CreatePrimitive();
        }

        protected virtual void OnDisable() {
            Destroy(primitive);
        }
    }

}