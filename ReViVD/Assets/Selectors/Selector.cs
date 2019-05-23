using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Revivd {

    [DisallowMultipleComponent]
    public class Selector : MonoBehaviour {
        public bool inverse = false;
        public bool erase = false;

        private bool _shown = false;
        public bool Shown {
            get => _shown;
            set {
                _shown = value;
                if (_shown) {
                    foreach (SelectorPart p in GetComponents<SelectorPart>())
                        p.Show();
                }
                else {
                    foreach (SelectorPart p in GetComponents<SelectorPart>())
                        p.Hide();
                }
            }
        }

        private void SeparateFromManager() {
            if (Persistent)
                SelectorManager.Instance.persistentSelectors[(int)Color].Remove(this);
            else if (SelectorManager.Instance.handSelectors[(int)Color] == this)
                SelectorManager.Instance.handSelectors[(int)Color] = null;

            Shown = false;
        }

        private void TryAttachingToManager() {
            if (Persistent) {
                SelectorManager.Instance.persistentSelectors[(int)Color].Add(this);
                Shown = true;
                wantsToAttach = false;
            }
            else {
                ref Selector hand = ref SelectorManager.Instance.handSelectors[(int)Color];
                if (hand == null || hand == this || !hand.isActiveAndEnabled) {
                    hand = this;
                    wantsToAttach = false;

                    if (SelectorManager.Instance.CurrentColor == Color) {
                        Shown = true;
                    }
                }
            }
        }

        private bool wantsToAttach = true;

        private bool old_persistent;
        [SerializeField]
        private bool _persistent = false;
        public bool Persistent {
            get => _persistent;
            set {
                if (value == _persistent && _persistent == old_persistent) //On rafraîchit la valeur si elle a été changée dans l'éditeur
                    return;

                SeparateFromManager();

                _persistent = value;
                old_persistent = _persistent;

                wantsToAttach = true;
            }
        }

        private SelectorManager.ColorGroup old_color;
        [SerializeField]
        private SelectorManager.ColorGroup _color = 0;
        public SelectorManager.ColorGroup Color {
            get => _color;
            set {
                if (value == _color && _color == old_color)
                    return;

                SeparateFromManager();

                _color = value;
                old_color = _color;

                wantsToAttach = true;
            }
        }

        private void OnEnable() {
            wantsToAttach = true;
        }

        private void OnDisable() {
            Shown = false;
            wantsToAttach = false;
        }

        private HashSet<Atom> handledRibbons = new HashSet<Atom>();
        public bool needsCheckedHighlightCleanup = false;

        public void Select() {
            if (!isActiveAndEnabled)
                return;

            Visualization viz = Visualization.Instance;
            HashSet<Atom> selectedRibbons = SelectorManager.Instance.selectedRibbons[(int)Color];

            foreach (SelectorPart p in GetComponents<SelectorPart>()) {
                if (!p.enabled)
                    continue;
                p.districtsToCheck.Clear();
                p.FindDistrictsToCheck();

                foreach (Atom a in p.ribbonsToCheck)
                    a.ShouldHighlightBecauseChecked((int)Color, false);
                p.ribbonsToCheck.Clear();

                foreach (int[] c in p.districtsToCheck) {
                    if (viz.districts.TryGetValue(c, out Visualization.District d)) {
                        foreach (Atom a in d.atoms_segment) {
                            if (a.ShouldDisplay) {
                                p.ribbonsToCheck.Add(a);
                                if (SelectorManager.Instance.HighlightChecked && !Persistent) {
                                    a.ShouldHighlightBecauseChecked((int)Color, true);
                                }
                            }
                        }
                    }
                }

                p.touchedRibbons.Clear();
                p.FindTouchedRibbons();
            }

            handledRibbons.Clear();

            foreach (SelectorPart p in GetComponents<SelectorPart>()) {
                if (!p.enabled)
                    continue;
                if (p.Positive) {
                    foreach (Atom a in p.touchedRibbons)
                        handledRibbons.Add(a);
                }
                else {
                    foreach (Atom a in p.touchedRibbons)
                        handledRibbons.Remove(a);
                }
            }

            if (inverse) { //Very inefficient code for now, may need an in-depth restructuration of the Viz/Path/Atom architecture
                List<Atom> allRibbons = new List<Atom>();
                foreach (Path p in viz.PathsAsBase) {
                    allRibbons.AddRange(p.AtomsAsBase);
                }
                HashSet<Atom> inversed = new HashSet<Atom>(allRibbons);
                inversed.ExceptWith(handledRibbons);

                if (erase)
                    selectedRibbons.ExceptWith(inversed);
                else
                    selectedRibbons.UnionWith(inversed);
            }
            else {
                if (erase)
                    selectedRibbons.ExceptWith(handledRibbons);
                else
                    selectedRibbons.UnionWith(handledRibbons);
            }

            if (SelectorManager.Instance.HighlightSelected) {
                foreach (Atom a in selectedRibbons) {
                    a.ShouldHighlightBecauseSelected((int)Color, true);
                }
            }

            if (SelectorManager.Instance.HighlightChecked)
                needsCheckedHighlightCleanup = true;
        }

        private void Awake() {
            old_color = Color;
            old_persistent = Persistent;
        }

        private void Update() {
            Color = _color; //Une update se fera si nécessaire (couleur changée dans l'éditeur)
            Persistent = _persistent; //idem

            if (wantsToAttach)
                TryAttachingToManager();
        }
    }
}