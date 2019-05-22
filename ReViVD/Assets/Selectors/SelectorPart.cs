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

        [SerializeField]
        protected GameObject primitive;

        protected abstract void CreatePrimitive();

        protected abstract void AttachToHand();

        private void DetachFromHand() {
            primitive.transform.parent = this.transform;
        }

        private bool _shown = true;
        public bool Shown {
            get => _shown;
            set {
                _shown = value;
                if (primitive != null) {
                    if (_shown)
                        Show();
                    else
                        Hide();
                }
            }
        }

        private void Show() {
            primitive.SetActive(true);
            primitive.GetComponent<Renderer>().material.color = SelectorManager.colors[(int)GetComponent<Selector>().Color];
            if (!GetComponent<Selector>().Persistent)
                AttachToHand();
            else
                DetachFromHand();
        }

        private void Hide() {
            primitive.SetActive(false);
        }

        public abstract void UpdatePrimitive();
        
        public abstract void FindDistrictsToCheck();

        public abstract void FindTouchedRibbons();

        protected virtual void OnEnable() {
            CreatePrimitive();
            Destroy(primitive.GetComponent<Collider>());
            if (Shown)
                Show();
            else
                Hide();
        }

        protected virtual void OnDisable() {
            Destroy(primitive);
        }
    }

}