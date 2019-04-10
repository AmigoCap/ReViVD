using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowAction : MonoBehaviour {

    bool doFollow = false;
    TimePath path;
    float startTime = 0;

	void Start () {
		
	}


    void Update() {
        if (Input.GetKeyDown(KeyCode.Return)) {
            if (!doFollow) {
                if (SelectorManager.Instance.Viz.selectedRibbons.Count == 0)
                    return;

                TimePath p = null;
                foreach (Atom a in SelectorManager.Instance.Viz.selectedRibbons) {
                    if (!ReferenceEquals(p, (TimePath)a.path))
                        if (p == null)
                            p = (TimePath)a.path;
                        else
                            return;
                }

                path = p;
                startTime = Time.time;
                doFollow = true;
                Movement.Instance.doJoystickControls = false;
            }
            else {
                doFollow = false;
                Movement.Instance.doJoystickControls = true;
            }
        }
        
        if (doFollow) {
            TimeAtom point1;
            TimeAtom point2;
            int index;
            Vector3 pos;
            float timeSinceFollow;
            timeSinceFollow = (Time.time - startTime) * 60 + path.AtomsAsTime[0].time;
            index = 0;


            if (timeSinceFollow < path.AtomsAsTime[path.AtomsAsTime.Count - 1].time) {
                while (index < (path.AtomsAsTime.Count - 1) && path.AtomsAsTime[index].time <= timeSinceFollow) {
                    index += 1;
                }

                point1 = path.AtomsAsTime[index - 1];
                point2 = path.AtomsAsTime[index];

                pos = point1.point + (point2.point - point1.point) * (timeSinceFollow - point1.time) / (point2.time - point1.time);
                Movement.Instance.transform.position = pos;
            }
            else {
                pos = path.AtomsAsTime[path.AtomsAsTime.Count - 1].point;
                Movement.Instance.transform.position = pos;
                doFollow = false;
                Movement.Instance.doJoystickControls = true;
            }


        }
    }
}
