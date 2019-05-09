﻿using UnityEngine;
using System.Collections;

namespace Revivd {

    public class MouseCamera : MonoBehaviour {

        /*
        Original code by Windexglow 11-13-10
        Debugged by the ReViVD team 2018
        */


        public float mainSpeed = 10.0f; //regular speed
        public float shiftAdd = 100.0f; //multiplied by how long shift is held.  Basically running
        public float maxShift = 1000.0f; //Maximum speed when holdin gshift
        public float camSens = 3f; //How sensitive it with mouse
        private Vector2 mouse;
        private float totalRun = 1.0f;
        public bool AZERTY = true;

        private void Start() {
            Cursor.lockState = CursorLockMode.Locked;
        }

        void Update() {
            if (Cursor.lockState == CursorLockMode.Locked) {
                mouse.x = Input.GetAxis("Mouse X");
                mouse.y = Input.GetAxis("Mouse Y");
                mouse = new Vector3(-mouse.y * camSens, mouse.x * camSens, 0);
                float vertAngle = transform.eulerAngles.x + mouse.x;
                if (vertAngle > 90 && vertAngle <= 180)
                    vertAngle = 90;
                else if (vertAngle < 270 && vertAngle >= 180)
                    vertAngle = 270;
                mouse = new Vector3(vertAngle, transform.eulerAngles.y + mouse.y, 0);
                transform.eulerAngles = mouse;
                mouse = Input.mousePosition;
                //Mouse  camera angle done.
            }

            //Keyboard commands
            Vector3 p = GetBaseInput();
            if (Input.GetKey(KeyCode.LeftShift)) {
                totalRun += Time.deltaTime;
                p = p * totalRun * shiftAdd;
                p.x = Mathf.Clamp(p.x, -maxShift, maxShift);
                p.y = Mathf.Clamp(p.y, -maxShift, maxShift);
                p.z = Mathf.Clamp(p.z, -maxShift, maxShift);
            }
            else {
                totalRun = Mathf.Clamp(totalRun * 0.5f, 1f, 1000f);
                p = p * mainSpeed;
            }

            p = p * Time.deltaTime;
            Vector3 newPosition = transform.position;
            if (Input.GetKey(KeyCode.Space)) { //If player wants to move on X and Z axis only
                transform.Translate(p);
                newPosition.x = transform.position.x;
                newPosition.z = transform.position.z;
                transform.position = newPosition;
            }
            else {
                transform.Translate(p);
            }

            if (Input.GetKey(KeyCode.Escape)) {
                Cursor.lockState = CursorLockMode.None;
            }

            if (Input.GetMouseButton(0)) {
                Cursor.lockState = CursorLockMode.Locked;
            }
        }

        private Vector3 GetBaseInput() { //returns the basic values, if it's 0 than it's not active.
            Vector3 p_Velocity = new Vector3();
            if (Input.GetKey(AZERTY ? KeyCode.Z : KeyCode.W)) {
                p_Velocity += new Vector3(0, 0, 1);
            }
            if (Input.GetKey(KeyCode.S)) {
                p_Velocity += new Vector3(0, 0, -1);
            }
            if (Input.GetKey(AZERTY ? KeyCode.Q : KeyCode.A)) {
                p_Velocity += new Vector3(-1, 0, 0);
            }
            if (Input.GetKey(KeyCode.D)) {
                p_Velocity += new Vector3(1, 0, 0);
            }
            if (Input.GetKey(AZERTY ? KeyCode.A : KeyCode.Q)) {
                p_Velocity += new Vector3(0, 1, 0);
            }
            if (Input.GetKey(KeyCode.E)) {
                p_Velocity += new Vector3(0, -1, 0);
            }
            return p_Velocity;
        }
    }

}