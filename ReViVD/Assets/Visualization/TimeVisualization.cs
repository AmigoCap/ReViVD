using System.Collections.Generic;
using UnityEngine;

namespace Revivd {

    public abstract class TimeVisualization : Visualization {
        protected abstract float InterpretTime(string word);

        public abstract IReadOnlyList<TimePath> PathsAsTime { get; }

        public float time = 0;
        private float old_time;
        public bool displayTimeSpheres = false;
        private bool old_displayTimeSpheres;

        public float timeSphereRadius = 1;
        private float old_timeSphereRadius;

        public bool doTimeAnim;
        public float animSpeed = 10;

        protected override void Awake() {
            base.Awake();

            old_displayTimeSpheres = displayTimeSpheres;
            old_time = time;
            old_timeSphereRadius = timeSphereRadius;
        }

        protected override void Update() {
            //if (Input.GetMouseButtonDown(0)) {
            //    doTimeAnim = !doTimeAnim;
            //}
            //if (Input.GetMouseButtonDown(1)) {
            //    time = 0;
            //}
            //if (Input.GetMouseButtonDown(2)) {
            //    displayTimeSpheres = !displayTimeSpheres;
            //}
            
            if (doTimeAnim && displayTimeSpheres) {
                time += animSpeed * Time.deltaTime;
            }

            if (displayTimeSpheres != old_displayTimeSpheres) {
                foreach (TimePath p in PathsAsTime) {
                    if (displayTimeSpheres)
                        p.TimeSphereTime = time;
                    p.DisplayTimeSphere(displayTimeSpheres);
                    old_displayTimeSpheres = displayTimeSpheres;
                }
            }

            if (displayTimeSpheres) {
                if (timeSphereRadius != old_timeSphereRadius) {
                    foreach (TimePath p in PathsAsTime) {
                        p.TimeSphereRadius = timeSphereRadius;
                    }
                    old_timeSphereRadius = timeSphereRadius;
                }

                if (time != old_time) {
                    foreach (TimePath p in PathsAsTime) {
                        p.TimeSphereTime = time;
                    }
                    old_time = time;
                }
            }

            base.Update();

            if (displayTimeSpheres) {
                foreach (TimePath p in PathsAsTime) {
                    if (p.UpdatedTrianglesThisFrame) {
                        p.TimeSphereTime = time;
                    }
                }
            }
        }
    }

    public abstract class TimePath : Path {
        public abstract IReadOnlyList<TimeAtom> AtomsAsTime { get; }

        private GameObject timeSphere;

        protected virtual void CreateTimeSphere() {
            timeSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(timeSphere.GetComponent<SphereCollider>());
            MeshRenderer renderer = timeSphere.GetComponent<MeshRenderer>();
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
            renderer.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
            timeSphere.transform.parent = this.transform;
            timeSphere.transform.localScale = Vector3.one * ((TimeVisualization)Visualization.Instance).timeSphereRadius;
            timeSphere.SetActive(shouldDisplayTimeSpheres);
        }

        public float TimeSphereRadius {
            set {
                if (timeSphere != null)
                    timeSphere.transform.localScale = Vector3.one * value;
            }
        }

        private float _timeSphereTime;
        public float TimeSphereTime {
            get => _timeSphereTime;
            set {
                _timeSphereTime = value;
                var it = AtomsAsTime.GetEnumerator();
                it.MoveNext();
                float t = it.Current.time;
                if (t > _timeSphereTime) { //First point is already too late
                    if (timeSphere != null) {
                        Destroy(timeSphere);
                        timeSphere = null;
                    }
                    return;
                }

                TimeAtom a = it.Current;
                while (it.MoveNext()) {
                    if (it.Current.time > _timeSphereTime) { //Next point is too late
                        if (!a.ShouldDisplay) {
                            if (timeSphere != null) {
                                Destroy(timeSphere);
                                timeSphere = null;
                            }
                            return;
                        }

                        if (timeSphere == null)
                            CreateTimeSphere();

                        Vector3 pos = a.point;
                        pos += (_timeSphereTime - a.time) / (it.Current.time - a.time) * (it.Current.point - a.point);
                        timeSphere.transform.localPosition = pos;
                        timeSphere.SetActive(true);
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
        }

        private bool shouldDisplayTimeSpheres = false;
        public void DisplayTimeSphere(bool state = true) {
            shouldDisplayTimeSpheres = state;
            if (timeSphere != null)
                timeSphere.SetActive(state);
        }

        protected override void Awake() {
            base.Awake();
        }

        public void SetTimeWindow(float startTime, float stopTime) { //Met à jour les atomes à afficher en fonction de si leur temps est dans la fenêtre recherchée
            foreach (TimeAtom a in AtomsAsTime) {
                a.ShouldDisplay = a.time > startTime && a.time < stopTime;
            }
        }

        public void RemoveTimeWindow() {
            foreach (TimeAtom a in AtomsAsTime) {
                a.ShouldDisplay = true;
            }
        }
    }

    public abstract class TimeAtom : Atom {
        public float time;
    }

}