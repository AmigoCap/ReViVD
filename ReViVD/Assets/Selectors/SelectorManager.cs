using System.Collections.Generic;
using UnityEngine;

namespace Revivd {

    [DisallowMultipleComponent]
    public class SelectorManager : MonoBehaviour
    {
        private static SelectorManager _instance;
        public static SelectorManager Instance { get { return _instance; } }

        public enum ColorGroup { Red = 0, Green, Blue, Yellow, Cyan, Magenta };
        public static readonly Color[] colors = { Color.red, Color.green, Color.blue, Color.yellow, Color.cyan, Color.magenta };
        public static readonly Color[] colors_dark = new Color[colors.Length];

        public Selector[] handSelectors = new Selector[colors.Length];
        public List<Selector>[] persistentSelectors = new List<Selector>[colors.Length];
        public HashSet<Atom>[] selectedRibbons = new HashSet<Atom>[colors.Length];

        public enum ControlMode { SelectionMode, CreationMode};
        public float creationGrowthCoefficient = 1f;
        public float creationMovementCoefficient = 1.5f;
        public float minCreationMovement = 2f;

        public GameObject leftSelectionCircleMask;
        public GameObject rightSelectionCircleMask;
        public GameObject leftCreationCircleMask;
        public GameObject rightCreationCircleMask;


        private ControlMode _currentControlMode = ControlMode.SelectionMode;
        public ControlMode CurrentControlMode {
            get => _currentControlMode;
            set {
                _currentControlMode = value;
                bool selectionMode = _currentControlMode == ControlMode.SelectionMode;
                leftSelectionCircleMask.SetActive(selectionMode);
                rightSelectionCircleMask.SetActive(selectionMode);
                leftCreationCircleMask.SetActive(!selectionMode);
                rightCreationCircleMask.SetActive(!selectionMode);

                Selector hs = handSelectors[(int)CurrentColor];
                if (hs != null && hs.isActiveAndEnabled) {
                    foreach (SelectorPart p in hs.GetComponents<SelectorPart>()) {
                        p.ShouldPollManualModifications = (!selectionMode);
                    }
                }
            }
        }

        public enum LogicMode { AND, OR };
        public LogicMode operationMode = LogicMode.OR;
        public HashSet<ColorGroup> operatingColors;

        [SerializeField]
        private bool s_highlightChecked = false;
        private bool _highlightChecked;
        public bool HighlightChecked {
            get => _highlightChecked;
            set {
                if (_highlightChecked == value)
                    return;

                _highlightChecked = value;
                s_highlightChecked = _highlightChecked;
            }
        }

        [SerializeField]
        private bool s_highlightSelected = true;
        private bool _highlightSelected;
        public bool HighlightSelected {
            get => _highlightSelected;
            set {
                if (_highlightSelected == value)
                    return;

                _highlightSelected = value;
                s_highlightSelected = _highlightSelected;

                for (int i = 0; i < colors.Length; i++) {
                    foreach (Atom a in selectedRibbons[i])
                        a.ShouldHighlightBecauseSelected(i, _highlightSelected);
                }
            }
        }

        [SerializeField]
        private ColorGroup s_currentColor = ColorGroup.Red;
        private ColorGroup _currentColor;
        public ColorGroup CurrentColor {
            get => _currentColor;
            set {
                if (_currentColor == value)
                    return;

                if (handSelectors[(int)_currentColor] != null)
                    handSelectors[(int)_currentColor].Shown = false;

                _currentColor = value;
                s_currentColor = _currentColor;

                if (handSelectors[(int)_currentColor] != null)
                    handSelectors[(int)_currentColor].Shown = true;
            }
        }

        public bool InverseMode {
            get => SteamVR_ControllerManager.LeftController.triggerPressed;
        }

        private bool ShouldSelect {
            get => SteamVR_ControllerManager.RightController.triggerPressed;
        }

        private void ChangeControlMode(SteamVR_TrackedController sender) {
            if (CurrentControlMode == ControlMode.SelectionMode)
                CurrentControlMode = ControlMode.CreationMode;
            else
                CurrentControlMode = ControlMode.SelectionMode;
        }

        private void SelectWithPersistents(SteamVR_TrackedController sender) {
            if (CurrentControlMode != ControlMode.SelectionMode)
                return;

            if (InverseMode) {
                ClearSelected(CurrentColor);
                return;
            }
            foreach (Selector s in persistentSelectors[(int)CurrentColor])
                s.Select();
        }

        private void MakePersistentCopyOfHand(SteamVR_TrackedController sender) {
            if (CurrentControlMode != ControlMode.SelectionMode)
                return;

            if (InverseMode) {//inverse mode
                int len = persistentSelectors[(int)CurrentColor].Count;
                if (len == 0)
                    return;
                Selector last = persistentSelectors[(int)CurrentColor][len - 1];
                persistentSelectors[(int)CurrentColor].RemoveAt(len - 1);
                Destroy(last.gameObject);
                return;
            }

            Selector s = handSelectors[(int)CurrentColor];
            if (s != null && s.isActiveAndEnabled) {
                GameObject go = Instantiate(s.gameObject, this.transform);

                go.name = "Persistent " + s.name;
                go.GetComponent<Selector>().Persistent = true;

                SelectorPart[] originalParts = s.GetComponents<SelectorPart>();
                SelectorPart[] newParts = go.GetComponents<SelectorPart>();
                for (int i = 0; i < originalParts.Length; i++) {
                    newParts[i].PrimitiveTransform.localRotation = originalParts[i].PrimitiveTransform.localRotation;
                    newParts[i].PrimitiveTransform.localPosition = originalParts[i].PrimitiveTransform.localPosition;
                    newParts[i].PrimitiveTransform.localScale = originalParts[i].PrimitiveTransform.localScale;
                }
            }                
        }

        public void DoLogicOperation() {
            if (InverseMode) { // Invert displayed ribbons
                foreach (Path p in Visualization.Instance.PathsAsBase) {
                    foreach (Atom a in p.AtomsAsBase)
                        a.ShouldDisplay = !a.ShouldDisplay;
                }

                return;
            }

            foreach (Path p in Visualization.Instance.PathsAsBase) {
                foreach (Atom a in p.AtomsAsBase)
                    a.ShouldDisplay = false;
            }

            HashSet<Path> pathsToKeep = new HashSet<Path>();
            if (operationMode == LogicMode.AND) {
                bool firstPass = true;
                foreach (ColorGroup c in operatingColors) {
                    if (firstPass) {
                        foreach (Atom a in selectedRibbons[(int)c])
                            pathsToKeep.Add(a.path);
                        firstPass = false;
                    }
                    else {
                        HashSet<Path> pathsToKeep2 = new HashSet<Path>();
                        foreach (Atom a in selectedRibbons[(int)c])
                            pathsToKeep2.Add(a.path);
                        pathsToKeep.IntersectWith(pathsToKeep2);
                    }
                }
            }
            else {
                foreach (ColorGroup c in operatingColors) {
                    foreach (Atom a in selectedRibbons[(int)c])
                        pathsToKeep.Add(a.path);
                }
            }

            foreach (Path p in pathsToKeep) {
                foreach (Atom a in p.AtomsAsBase)
                    a.ShouldDisplay = true;
            }

        }

        public void ClearSelected(ColorGroup color) {
            if (HighlightSelected) {
                foreach (Atom a in selectedRibbons[(int)color])
                    a.ShouldHighlightBecauseSelected((int)color, false);
            }
            selectedRibbons[(int)color].Clear();
        }

        private void OnEnable() {
            SteamVR_ControllerManager.RightController.Gripped += SelectWithPersistents;
            SteamVR_ControllerManager.RightController.MenuButtonClicked += MakePersistentCopyOfHand;
            SteamVR_ControllerManager.LeftController.MenuButtonClicked += ChangeControlMode;
        }

        private void OnDisable() {
            if (SteamVR_ControllerManager.RightController != null) {
                SteamVR_ControllerManager.RightController.Gripped -= SelectWithPersistents;
                SteamVR_ControllerManager.RightController.MenuButtonClicked -= MakePersistentCopyOfHand;
            }

            if (SteamVR_ControllerManager.LeftController != null) {
                SteamVR_ControllerManager.LeftController.MenuButtonClicked -= ChangeControlMode;
            }
        }

        private void Awake() {
            if (_instance != null) {
                Debug.LogWarning("Multiple instances of selector manager singleton");
            }
            _instance = this;

            for (int i = 0; i < colors.Length; i++) {
                colors_dark[i] = colors[i] / 3;
                persistentSelectors[i] = new List<Selector>();
                selectedRibbons[i] = new HashSet<Atom>();
            }

            _currentColor = s_currentColor;
            _highlightChecked = s_highlightChecked;
            _highlightSelected = s_highlightSelected;

            operatingColors = new HashSet<ColorGroup>();
        }

        private void Update() {
            CurrentColor = s_currentColor; //Triggers the property if _currentColor was changed in the editor
            HighlightChecked = s_highlightChecked;
            HighlightSelected = s_highlightSelected;

            if (!ShouldSelect) { //clean up highlights
                foreach (Selector s in handSelectors)
                    if (s != null && s.needsCheckedHighlightCleanup) {
                        foreach (SelectorPart p in s.GetComponents<SelectorPart>())
                            foreach (Atom a in p.RibbonsToCheck)
                                a.ShouldHighlightBecauseChecked((int)s.Color, false);
                        s.needsCheckedHighlightCleanup = false;
                    }
            }
            

            if (CurrentControlMode == ControlMode.SelectionMode) { // Selection
                Selector hs = handSelectors[(int)CurrentColor];
                if (hs != null && hs.isActiveAndEnabled) {
                    if (ShouldSelect) {
                        hs.Select(InverseMode);
                    }
                }
            }
        }
    }

}