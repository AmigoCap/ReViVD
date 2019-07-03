using System.Collections.Generic;
using UnityEngine;

namespace Revivd {

    public abstract class TimeVisualization : Visualization {
        protected abstract float InterpretTime(string word);

        public abstract IReadOnlyList<TimePath> PathsAsTime { get; }

        public bool displayTimeSpheres = false;

        public float globalTime = 0;

        public bool useGlobalTime = true;

        public bool doSphereDrop = false;

        private float old_timeSphereRadius;
        public float timeSphereRadius = 1;

        public bool doTimeSphereAnimation = false;
        public float timeSphereAnimationSpeed = 10;

        private bool old_traceTimeSpheres;
        public bool traceTimeSpheres = false;

        protected override void Awake() {
            base.Awake();

            old_timeSphereRadius = timeSphereRadius;
            old_traceTimeSpheres = traceTimeSpheres;
        }

        public void DropSpheres() {
            foreach (TimePath p in PathsAsTime) {
                p.timeSphereDropped = false;
                p.timeSphereTime = timeSphereAnimationSpeed < 0 ? float.NegativeInfinity : float.PositiveInfinity;
            }
            foreach (TimeAtom a in SelectorManager.Instance.selectedRibbons[(int)SelectorManager.Instance.CurrentColor]) {
                if (a.ShouldDisplay) {
                    TimePath p = (TimePath)a.path;
                    if (timeSphereAnimationSpeed < 0) {
                        p.timeSphereTime = Mathf.Max(p.timeSphereTime, a.time);
                    }
                    else {
                        p.timeSphereTime = Mathf.Min(p.timeSphereTime, a.time);
                    }
                    p.timeSphereDropped = true;
                }
            }
        }

        protected override void Update() {
            base.Update();

            if (!traceTimeSpheres && old_traceTimeSpheres) {
                foreach (TimePath p in PathsAsTime) {
                    foreach (TimeAtom a in p.AtomsAsTime) {
                        a.ShouldDisplayBecauseTime = true;
                    }
                }
                old_traceTimeSpheres = false;
            }

            if (displayTimeSpheres) {
                if (doSphereDrop) {
                    DropSpheres();
                    doSphereDrop = false;
                }

                if (timeSphereRadius != old_timeSphereRadius) {
                    foreach (TimePath p in PathsAsTime) {
                        p.UpdateTimeSphereRadius();
                    }
                    old_timeSphereRadius = timeSphereRadius;
                }

                if (traceTimeSpheres && !old_traceTimeSpheres) {
                    foreach (TimePath p in PathsAsTime) {
                        foreach (TimeAtom a in p.AtomsAsTime) {
                            a.ShouldDisplayBecauseTime = false;
                        }
                    }
                    old_traceTimeSpheres = true;
                }

                if (useGlobalTime && doTimeSphereAnimation)
                    globalTime += timeSphereAnimationSpeed * Time.deltaTime;

            }

            foreach (TimePath p in PathsAsTime) {
                p.UpdateTimeSphere();
            }
        }
    }

    public abstract class TimePath : Path {
        public abstract IReadOnlyList<TimeAtom> AtomsAsTime { get; }

        private GameObject timeSphere = null;

        public float timeSphereTime = 0;
        public bool timeSphereDropped = false;

        protected virtual void CreateTimeSphere() {
            timeSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(timeSphere.GetComponent<SphereCollider>());
            MeshRenderer renderer = timeSphere.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            timeSphere.transform.parent = this.transform;
            timeSphere.transform.localScale = Vector3.one * ((TimeVisualization)Visualization.Instance).timeSphereRadius;
        }

        public void UpdateTimeSphereRadius() {
            if (timeSphere != null) {
                timeSphere.transform.localScale = Vector3.one * ((TimeVisualization)Visualization.Instance).timeSphereRadius;
            }
        }

        public void UpdateTimeSphere() {
            TimeVisualization viz = (TimeVisualization)Visualization.Instance;

            if (!viz.displayTimeSpheres) {
                if (timeSphere != null) {
                    Destroy(timeSphere);
                    timeSphere = null;
                }
                return;
            }

            if (viz.useGlobalTime) {
                timeSphereDropped = false;
                timeSphereTime = viz.globalTime;
                UpdateTimeSpherePosition();
            }
            else if (timeSphereDropped) {
                if (viz.doTimeSphereAnimation)
                    timeSphereTime += viz.timeSphereAnimationSpeed * Time.deltaTime;
                UpdateTimeSpherePosition();
            }
            else {
                if (timeSphere != null) {
                    Destroy(timeSphere);
                    timeSphere = null;
                }
            }

        }

        private void UpdateTimeSpherePosition() {
            var it = AtomsAsTime.GetEnumerator();
            it.MoveNext();
            float t = it.Current.time;
            if (t > timeSphereTime) { //First point is already too late
                if (timeSphere != null) {
                    Destroy(timeSphere);
                    timeSphere = null;
                }
                return;
            }

            TimeVisualization viz = (TimeVisualization)Visualization.Instance;
            TimeAtom a = it.Current;
            while (it.MoveNext()) {
                if (it.Current.time > timeSphereTime) { //Next point is too late
                    if (!a.ShouldDisplayBecauseSelected) {
                        if (timeSphere != null) {
                            Destroy(timeSphere);
                            timeSphere = null;
                        }
                        return;
                    }

                    if (timeSphere == null)
                        CreateTimeSphere();

                    Vector3 pos = a.point;
                    pos += (timeSphereTime - a.time) / (it.Current.time - a.time) * (it.Current.point - a.point);
                    timeSphere.transform.localPosition = pos;
                    timeSphere.SetActive(true);
                    if (viz.traceTimeSpheres) {
                        a.ShouldDisplayBecauseTime = true;
                    }
                    return;
                }
                a = it.Current;
            }

            //Reached the end while still being too early
            if (timeSphere != null) {
                Destroy(timeSphere);
                timeSphere = null;
            }
        }

        public void SetTimeWindow(float startTime, float stopTime) { //Met à jour les atomes à afficher en fonction de si leur temps est dans la fenêtre recherchée
            foreach (TimeAtom a in AtomsAsTime) {
                a.ShouldDisplayBecauseTime = a.time > startTime && a.time < stopTime;
            }
        }

        public void RemoveTimeWindow() {
            foreach (TimeAtom a in AtomsAsTime) {
                a.ShouldDisplayBecauseTime = true;
            }
        }
    }

    public abstract class TimeAtom : Atom {
        public float time;

        private bool shouldDisplay_time = true;

        public override bool ShouldDisplay {
            get => base.ShouldDisplay && shouldDisplay_time;
        }

        public bool ShouldDisplayBecauseTime {
            get => shouldDisplay_time;
            set {
                bool wasDisplayed = ShouldDisplay;
                shouldDisplay_time = value;
                if (wasDisplayed != ShouldDisplay) {
                    path.needsTriangleUpdate = true;
                    if (!wasDisplayed)
                        path.needsColorUpdate = true;
                }
            }
        }
    }

}