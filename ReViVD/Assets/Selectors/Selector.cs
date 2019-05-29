using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Revivd {

    [DisallowMultipleComponent]
    public class Selector : MonoBehaviour {
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

        public void ScaleUp() {
            foreach (SelectorPart p in GetComponents<SelectorPart>()) {
                p.Scale(SelectorManager.Instance.sizeExponent);
            }
        }

        public void ScaleDown() {
            foreach (SelectorPart p in GetComponents<SelectorPart>()) {
                p.Scale(1 / SelectorManager.Instance.sizeExponent);
            }
        }

        public void UpdatePosition() {
            foreach (SelectorPart p in GetComponents<SelectorPart>()) {
                p.Translate(SteamVR_ControllerManager.RightController.Joystick.y * Time.deltaTime * 100, 0, 0);
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

        [SerializeField]
        private bool s_persistent = false;
        private bool _persistent;
        public bool Persistent {
            get => _persistent;
            set {
                if (value == _persistent)
                    return;

                SeparateFromManager();

                _persistent = value;
                s_persistent = _persistent;

                wantsToAttach = true;
            }
        }

        [SerializeField]
        private SelectorManager.ColorGroup s_color = 0;
        private SelectorManager.ColorGroup _color;
        public SelectorManager.ColorGroup Color {
            get => _color;
            set {
                if (value == _color)
                    return;

                SeparateFromManager();
                
                _color = value;
                s_color = _color;

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

        public void Select(bool erase = false) {
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

            if (erase) {
                selectedRibbons.ExceptWith(handledRibbons);

                foreach (Atom a in handledRibbons) {
                    a.ShouldHighlightBecauseSelected((int)Color, false);
                }
            }
            else {
                selectedRibbons.UnionWith(handledRibbons);

                if (SelectorManager.Instance.HighlightSelected) {
                    foreach (Atom a in handledRibbons) {
                        a.ShouldHighlightBecauseSelected((int)Color, true);
                    }
                }
            }

            if (SelectorManager.Instance.HighlightChecked)
                needsCheckedHighlightCleanup = true;
        }

        private void Awake() {
            _color = s_color;
            _persistent = s_persistent;
        }

        private void Update() {
            Color = s_color; //Une update se fera si nécessaire (couleur changée dans l'éditeur)
            Persistent = s_persistent; //idem

            if (wantsToAttach)
                TryAttachingToManager();
        }
    }
}