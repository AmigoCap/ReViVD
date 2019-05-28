//======= Copyright (c) Valve Corporation, All rights reserved. ===============
using UnityEngine;
using Valve.VR;

public delegate void ClickedEventHandler(SteamVR_TrackedController sender);

public class SteamVR_TrackedController : MonoBehaviour {
    public uint controllerIndex;
    public VRControllerState_t controllerState;
    public bool triggerPressed = false;
    public bool steamPressed = false;
    public bool menuPressed = false;
    public bool padPressed = false;
    public bool padTouched = false;
    public bool gripped = false;

    public event ClickedEventHandler MenuButtonClicked;
    public event ClickedEventHandler MenuButtonUnclicked;
    public event ClickedEventHandler TriggerClicked;
    public event ClickedEventHandler TriggerUnclicked;
    public event ClickedEventHandler JoystickClicked;
    public event ClickedEventHandler PadClicked;
    public event ClickedEventHandler PadUnclicked;
    public event ClickedEventHandler PadTouched;
    public event ClickedEventHandler PadUntouched;
    public event ClickedEventHandler Gripped;
    public event ClickedEventHandler Ungripped;

    Vector2 ApplyDead(float x, float y, float d) {
        return new Vector2((x > -d && x < d) ? 0 : x, (y > -d && y < d) ? 0 : y);

    }

    const float joystickDead = 0.2f;
    Vector2 joystick = Vector2.zero;
    bool joystickNeedsUpdate = true;
    public Vector2 Joystick {
        get {
            if (joystickNeedsUpdate) {
                joystick = ApplyDead(controllerState.rAxis2.x, controllerState.rAxis2.y, joystickDead);
                joystickNeedsUpdate = false;
            }
            return joystick;
        }
    }

    Vector2 pad = Vector2.zero;
    bool padNeedsUpdate = true;
    public Vector2 Pad {
        get {
            if (padNeedsUpdate) {
                pad.Set(controllerState.rAxis0.x, controllerState.rAxis0.y);
                padNeedsUpdate = false;
            }
            return pad;
        }
    }

    public float Shoulder {
        get {
            return controllerState.rAxis1.x;
        }
    }

    // Use this for initialization
    protected virtual void Start() {
        if (this.GetComponent<SteamVR_TrackedObject>() == null) {
            gameObject.AddComponent<SteamVR_TrackedObject>();
        }

        if (controllerIndex != 0) {
            this.GetComponent<SteamVR_TrackedObject>().index = (SteamVR_TrackedObject.EIndex)controllerIndex;
            if (this.GetComponent<SteamVR_RenderModel>() != null) {
                this.GetComponent<SteamVR_RenderModel>().index = (SteamVR_TrackedObject.EIndex)controllerIndex;
            }
        }
        else {
            controllerIndex = (uint)this.GetComponent<SteamVR_TrackedObject>().index;
        }
    }

    public void SetDeviceIndex(int index) {
        this.controllerIndex = (uint)index;
    }

    public virtual void OnTriggerClicked() {
        if (TriggerClicked != null)
            TriggerClicked(this);
    }

    public virtual void OnTriggerUnclicked() {
        if (TriggerUnclicked != null)
            TriggerUnclicked(this);
    }

    public virtual void OnMenuClicked() {
        if (MenuButtonClicked != null)
            MenuButtonClicked(this);
    }

    public virtual void OnMenuUnclicked() {
        if (MenuButtonUnclicked != null)
            MenuButtonUnclicked(this);
    }

    public virtual void OnSteamClicked() {
        if (JoystickClicked != null)
            JoystickClicked(this);
    }

    public virtual void OnPadClicked() {
        if (PadClicked != null)
            PadClicked(this);
    }

    public virtual void OnPadUnclicked() {
        if (PadUnclicked != null)
            PadUnclicked(this);
    }

    public virtual void OnPadTouched() {
        if (PadTouched != null)
            PadTouched(this);
    }

    public virtual void OnPadUntouched() {
        if (PadUntouched != null)
            PadUntouched(this);
    }

    public virtual void OnGripped() {
        if (Gripped != null)
            Gripped(this);
    }

    public virtual void OnUngripped() {
        if (Ungripped != null)
            Ungripped(this);
    }

    // Update is called once per frame
    protected virtual void Update() {
        var system = OpenVR.System;
        if (system != null && system.GetControllerState(controllerIndex, ref controllerState, (uint)System.Runtime.InteropServices.Marshal.SizeOf(typeof(VRControllerState_t)))) {
            ulong trigger = controllerState.ulButtonPressed & (1UL << ((int)EVRButtonId.k_EButton_SteamVR_Trigger));
            if (trigger > 0L && !triggerPressed) {
                triggerPressed = true;
                OnTriggerClicked();

            }
            else if (trigger == 0L && triggerPressed) {
                triggerPressed = false;
                OnTriggerUnclicked();
            }

            ulong grip = controllerState.ulButtonPressed & (1UL << ((int)EVRButtonId.k_EButton_Grip));
            if (grip > 0L && !gripped) {
                gripped = true;
                OnGripped();

            }
            else if (grip == 0L && gripped) {
                gripped = false;
                OnUngripped();
            }

            ulong pad = controllerState.ulButtonPressed & (1UL << ((int)EVRButtonId.k_EButton_SteamVR_Touchpad));
            if (pad > 0L && !padPressed) {
                padPressed = true;
                OnPadClicked();
            }
            else if (pad == 0L && padPressed) {
                padPressed = false;
                OnPadUnclicked();
            }

            ulong menu = controllerState.ulButtonPressed & (1UL << ((int)EVRButtonId.k_EButton_ApplicationMenu));
            if (menu > 0L && !menuPressed) {
                menuPressed = true;
                OnMenuClicked();
            }
            else if (menu == 0L && menuPressed) {
                menuPressed = false;
                OnMenuUnclicked();
            }

            pad = controllerState.ulButtonTouched & (1UL << ((int)EVRButtonId.k_EButton_SteamVR_Touchpad));
            if (pad > 0L && !padTouched) {
                padTouched = true;
                OnPadTouched();

            }
            else if (pad == 0L && padTouched) {
                padTouched = false;
                OnPadUntouched();
            }
        }

        joystickNeedsUpdate = true;
        padNeedsUpdate = true;
    }
}
