﻿using UnityEngine;
using System.IO;
using System;
using System.Globalization;

namespace Revivd {
    [DisallowMultipleComponent]
    public class Logger : MonoBehaviour {
        private static Logger _instance;
        public static Logger Instance { get { return _instance; } }

        string posLogBuffer = "";
        string eventLogBuffer = "";

        public static NumberFormatInfo nfi = new NumberFormatInfo();

        void Awake() {
            if (_instance != null) {
                Debug.LogWarning("Multiple instances of logger singleton");
            }
            _instance = this;
        }

        void LogPosition() {
            Vector3 pos = Camera.main.transform.position;
            posLogBuffer += pos.x.ToString(nfi) + ',' + pos.y.ToString(nfi) + ',' + pos.z.ToString(nfi) + '\n';
        }

        public void LogEvent(string eventString) {
            eventLogBuffer += Time.time.ToString(nfi) + ',' + eventString + '\n';
        }

        public static readonly string[] colorString = { "R", "G", "B", "Y", "C", "M" };
        public void LogOperation(int pathsKept) {
            string s = "OP,";
            s += (SelectorManager.Instance.operationMode == SelectorManager.LogicMode.AND ? "AND" : "OR") + ',';
            foreach (SelectorManager.ColorGroup c in SelectorManager.Instance.operatingColors)
                s += colorString[(int)c];
            s += ',' + pathsKept.ToString();
            LogEvent(s);
        }

        public void LogControlModeSwitch(SelectorPart p) {
            string s = SelectorManager.Instance.CurrentControlMode == SelectorManager.ControlMode.CreationMode ? "CRMODE_IN," : "CRMODE_OUT,";
            s += colorString[(int)SelectorManager.Instance.CurrentColor] + ',';
            LogEvent(s + p.GetLogString());
        }

        public void LogPersistentAdded(SelectorPart p) {
            string s = "+PRST,";
            for (int i = 0; i < 3; i++)
                s += p.PrimitiveTransform.position[i].ToString(nfi) + ',';
            for (int i = 0; i < 3; i++)
                s += p.PrimitiveTransform.eulerAngles[i].ToString(nfi) + ',';
            s += colorString[(int)SelectorManager.Instance.CurrentColor] + ',';
            s += p.GetLogString();
            LogEvent(s);
        }

        public string dirname;

        void Start() {
            DateTime now = DateTime.Now;
            string dir = "ReViVD Output/" + now.Day.ToString("00") + '-' + now.Month.ToString("00") + '-' + now.Year.ToString().Substring(2, 2) + "_" + now.Hour.ToString("00") + 'h' + now.Minute.ToString("00");
            Directory.CreateDirectory(System.IO.Path.Combine(System.IO.Directory.GetCurrentDirectory(), dir));
            dirname = new FileInfo(dir).FullName;

            nfi.NumberDecimalSeparator = ".";

            InvokeRepeating("LogPosition", 0, 0.5f);
        }

        private void OnDisable() {
            CancelInvoke();
            using (StreamWriter posLog = new StreamWriter(System.IO.Path.Combine(dirname, "position.csv")))
            using (StreamWriter eventLog = new StreamWriter(System.IO.Path.Combine(dirname, "events.csv"))) {
                posLog.Write(posLogBuffer);
                eventLog.Write(eventLogBuffer);
            }
        }
    }

}