using System.Collections.Generic;

namespace Revivd {

    public abstract class TimeVisualization : Visualization {
        protected abstract float InterpretTime(string word);

        public abstract IReadOnlyList<TimePath> PathsAsTime { get; }
    }

    public abstract class TimePath : Path {
        public abstract IReadOnlyList<TimeAtom> AtomsAsTime { get; }

        public void SetTimeWindow(float startTime, float stopTime) { //Met à jour les atomes à afficher en fonction de si leur temps est dans la fenêtre recherchée
            bool shouldUpdateTriangles = false;
            foreach (TimeAtom a in AtomsAsTime) {
                if (a.shouldDisplay != (a.time > startTime && a.time < stopTime)) {
                    a.shouldDisplay = !a.shouldDisplay;
                    shouldUpdateTriangles = true;
                }
            }
            if (shouldUpdateTriangles)
                GenerateTriangles();
        }

        public void RemoveTimeWindow() {
            bool shouldUpdateTriangles = false;
            foreach (TimeAtom a in AtomsAsTime) {
                if (!a.shouldDisplay) {
                    a.shouldDisplay = true;
                    shouldUpdateTriangles = true;
                }
            }
            if (shouldUpdateTriangles)
                GenerateTriangles();
        }
    }

    public abstract class TimeAtom : Atom {
        public float time;
    }

}