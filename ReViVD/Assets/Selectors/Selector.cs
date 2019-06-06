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

        public bool needsCheckedHighlightCleanup = false;

        public void Select(bool erase = false) {
            if (!isActiveAndEnabled)
                return;

            List<SelectorPart> parts = new List<SelectorPart>(GetComponents<SelectorPart>());
            parts.RemoveAll(p => !p.enabled);

            if (parts.Count == 0)
                return;

            Visualization viz = Visualization.Instance;
            HashSet<Atom> selectedRibbons = SelectorManager.Instance.selectedRibbons[(int)Color];

            foreach (SelectorPart p in parts) {
                if (needsCheckedHighlightCleanup) {
                    needsCheckedHighlightCleanup = false;
                    foreach (Atom a in p.RibbonsToCheck)
                        a.ShouldHighlightBecauseChecked((int)Color, false);
                }

                p.FindTouchedRibbons();

                if (SelectorManager.Instance.HighlightChecked && !Persistent) {
                    foreach (Atom a in p.RibbonsToCheck)
                        a.ShouldHighlightBecauseChecked((int)Color, true);
                }
            }

            IEnumerable<Atom> handledRibbons;

            if (parts.Count > 1) {
                HashSet<Atom> handledRibbonsSet = new HashSet<Atom>();

                foreach (SelectorPart p in GetComponents<SelectorPart>()) {
                    if (!p.enabled)
                        continue;
                    if (p.Positive) {
                        handledRibbonsSet.UnionWith(p.TouchedRibbons);
                    }
                    else {
                        handledRibbonsSet.ExceptWith(p.TouchedRibbons);
                    }
                }

                handledRibbons = handledRibbonsSet;
            }
            else {
                handledRibbons = parts[0].TouchedRibbons;
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

        private void OnEnable() {
            wantsToAttach = true;
        }

        private void OnDisable() {
            Shown = false;
            wantsToAttach = false;
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