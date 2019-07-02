using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;

namespace Revivd {

    public class CuboidPart : SelectorPart {
        private Vector3 initialHandOffset;
        private Vector3 initialSize;

        public Vector3 handOffset = new Vector3(0f, 0f, 4f);
        public Vector3 size = new Vector3(1f, 1f, 1f);

        protected override void CreatePrimitive() {
            initialSize = size;
            initialHandOffset = handOffset;
            primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _type = PrimitiveType.Cube;
        }
        
        protected override void UpdatePrimitive() {
            primitive.transform.localPosition = handOffset;
            primitive.transform.localScale = size;
            if (!GetComponent<Selector>().Persistent)
                primitive.transform.eulerAngles = new Vector3(0, SteamVR_ControllerManager.RightController.transform.eulerAngles.y, 0);
        }

        protected override void UpdateManualModifications() {

            size += (SelectorManager.Instance.InverseMode ? 1 : -1) * size * SteamVR_ControllerManager.RightController.Shoulder * SelectorManager.Instance.creationGrowthCoefficient * Time.deltaTime;

            handOffset.x += Mathf.Max(Mathf.Abs(handOffset.x), SelectorManager.Instance.minCreationMovement) * SelectorManager.Instance.creationMovementCoefficient * SteamVR_ControllerManager.RightController.Joystick.x * Time.deltaTime;
            handOffset.y += Mathf.Max(Mathf.Abs(handOffset.y), SelectorManager.Instance.minCreationMovement) * SelectorManager.Instance.creationMovementCoefficient * SteamVR_ControllerManager.RightController.Joystick.y * Time.deltaTime;

            size.x += size.x * SelectorManager.Instance.creationGrowthCoefficient * SteamVR_ControllerManager.LeftController.Joystick.x * Time.deltaTime;
            size.y += size.y * SelectorManager.Instance.creationGrowthCoefficient * SteamVR_ControllerManager.LeftController.Joystick.y * Time.deltaTime;

            if (SelectorManager.Instance.InverseMode) {
                if (SteamVR_ControllerManager.LeftController.padPressed) {
                    size = initialSize;
                }

                if (SteamVR_ControllerManager.RightController.padPressed) {
                    handOffset = initialHandOffset;
                }
            }
            else {
                if (SteamVR_ControllerManager.RightController.padPressed) {
                    handOffset.z += (SteamVR_ControllerManager.RightController.Pad.y >= 0 ? 1 : -1) * Mathf.Max(Mathf.Abs(handOffset.z), SelectorManager.Instance.minCreationMovement) * SelectorManager.Instance.creationMovementCoefficient * Time.deltaTime;
                }

                if (SteamVR_ControllerManager.LeftController.padPressed) {
                    size.z += (SteamVR_ControllerManager.LeftController.Pad.y >= 0 ? 1 : -1) * size.z * SelectorManager.Instance.creationMovementCoefficient * Time.deltaTime;
                }
            }
        }

        protected override void ParseRibbonsToCheck() {
            foreach (Atom a in ribbonsToCheck) {
                if (CuboidTouchesRibbon(a)) {
                    touchedRibbons.Add(a);
                }
            }
        }

        private bool CuboidTouchesRibbon(Atom a) {
            //All computations are made in the primitive's coordinates, which changes the scale of things (like the ribbons' radii)

            if (!a.path.specialRadii.TryGetValue(a.indexInPath, out float radius))
                radius = a.path.baseRadius;
            Vector3 scaledRadius = Vector3.one * radius;
            for (int i = 0; i < 3; i++) {
                scaledRadius[i] /= primitive.transform.localScale[i];
            }

            Vector3 cuboidDimensions = Vector3.one / 2; //Constant in the primitive's coordinate system

            //Segment in the world space coordinates
            Vector3 b = a.path.transform.TransformPoint(a.point);
            Vector3 c = a.path.transform.TransformPoint(a.path.AtomsAsBase[a.indexInPath + 1].point);
            //and then in the primitive's coordinates
            Vector3 bc = primitive.transform.InverseTransformPoint(b);
            Vector3 cc = primitive.transform.InverseTransformPoint(c);

            Vector3 mc = (bc + cc) / 2; //midpoint vector of the segment (origin is the cuboid's center)
            Vector3 l = bc - mc; 
            Vector3 lext = new Vector3(Mathf.Abs(l.x), Mathf.Abs(l.y), Mathf.Abs(l.z));//extent vector of the segment
            
            //Separating axis test: axis = separating axis? 
            if (Mathf.Abs(mc.x) >  cuboidDimensions.x + scaledRadius.x + lext.x) return false;
            if (Mathf.Abs(mc.y) >  cuboidDimensions.y + scaledRadius.y + lext.y) return false;
            if (Mathf.Abs(mc.z) >  cuboidDimensions.z + scaledRadius.z + lext.z) return false;

            bool first = true;
            bool second = true;

            if (Mathf.Abs(mc.y * l.z - mc.z * l.y) > ((cuboidDimensions.y + scaledRadius.y) * lext.z + cuboidDimensions.z * lext.y)) first = false;
            if (Mathf.Abs(mc.y * l.z - mc.z * l.y) > (cuboidDimensions.y * lext.z + (cuboidDimensions.z + scaledRadius.z) * lext.y)) second = false;

            if (!first & !second) return false;
            first = true;
            second = true;

            if (Mathf.Abs(mc.x * l.z - mc.z * l.x) > ((cuboidDimensions.x + scaledRadius.x) * lext.z + cuboidDimensions.z * lext.x)) first = false;
            if (Mathf.Abs(mc.x * l.z - mc.z * l.x) > (cuboidDimensions.x * lext.z + (cuboidDimensions.z + scaledRadius.z) * lext.x)) second = false;

            if (!first & !second) return false;
            first = true;
            second = true;

            if (Mathf.Abs(mc.x * l.y - mc.y * l.x) > ((cuboidDimensions.x + scaledRadius.x) * lext.y + cuboidDimensions.y * lext.x)) first = false;
            if (Mathf.Abs(mc.x * l.y - mc.y * l.x) > (cuboidDimensions.x * lext.y + (cuboidDimensions.y + scaledRadius.z) * lext.x)) second = false;

            if (!first & !second) return false;

            return true;
        }

    }
}