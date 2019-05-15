using System.Collections.Generic;

namespace Revivd {

    public abstract class TimeVisualization : Visualization {
        protected abstract float InterpretTime(string word);

        public abstract IReadOnlyList<TimePath> PathsAsTime { get; }
    }

    public abstract class TimePath : Path {
        public abstract IReadOnlyList<TimeAtom> AtomsAsTime { get; }

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