#if false
using System.Collections.Generic;
using UnityEngine;



public static class ClickThruBlocker
{
    // Most of this is from JanitorsCloset, ImportExportSelect.cs

    public class CTBWin
    {
        public Rect rect;
        int id;

        bool weLockedEditorInputs = false;
        bool weLockedFlightInputs = false;
        string windowName;
        string lockName;

        public CTBWin(Rect screenRect)
        {
            this.rect = screenRect;
        }

        public CTBWin(float x, float y, float width, float height)
        {
            this.rect = new Rect(x, y, width, height);
        }

        public CTBWin(int id, Rect screenRect, string winName, string lockName)
        {
            this.id = id;
            this.rect = screenRect;
            this.windowName = winName;
            this.lockName = lockName;

        }

        public void SetLockString(string s)
        {
            this.lockName = s;
        }

        private bool MouseIsOverWindow()
        {
            return rect.Contains(new Vector2(Input.mousePosition.x, Screen.height - Input.mousePosition.y));
        }

        //Lifted this more or less directly from the Kerbal Engineer source. Thanks cybutek!
        internal void PreventEditorClickthrough()
        {
            Log.Info("ClickThruBlocker: PreventEditorClickthrough");
            bool mouseOverWindow = MouseIsOverWindow();
            if (!weLockedEditorInputs && mouseOverWindow)
            {
                Log.Info("ClickThruBlocker: PreventEditorClickthrough, locking");
                EditorLogic.fetch.Lock(true, true, true, lockName);
                weLockedEditorInputs = true;
            }
            if (!weLockedEditorInputs || mouseOverWindow) return;
            Log.Info("ClickThruBlocker: PreventEditorClickthrough, unlocking");
            EditorLogic.fetch.Unlock(lockName);
            weLockedEditorInputs = false;
        }

        // Following lifted from MechJeb
        internal void PreventInFlightClickthrough()
        {
            Log.Info("ClickThruBlocker: PreventInFlightClickthrough");
            bool mouseOverWindow = MouseIsOverWindow();
            if (!weLockedFlightInputs && mouseOverWindow && !Input.GetMouseButton(1))
            {
                Log.Info("ClickThruBlocker: PreventInFlightClickthrough, locking");

                InputLockManager.SetControlLock(ControlTypes.ALLBUTCAMERAS, lockName);
                weLockedFlightInputs = true;
            }
            if (weLockedFlightInputs && !mouseOverWindow)
            {
                Log.Info("ClickThruBlocker: PreventInFlightClickthrough, unlocking");
                InputLockManager.RemoveControlLock(lockName);
                weLockedFlightInputs = false;
            }
        }
        void OnDestroy()
        {
            Log.Info("ClickThruBlocker: OnDestroy");
            winList.Remove(id);
            if (weLockedEditorInputs)
            {
                EditorLogic.fetch.Unlock(lockName);
                weLockedEditorInputs = false;
            }
            if (weLockedFlightInputs)
            {
                InputLockManager.RemoveControlLock(lockName);
                weLockedFlightInputs = false;
            }
        }

    }

    static Dictionary<int, CTBWin> winList = new Dictionary<int, CTBWin>();

    static CTBWin UpdateList(int id, Rect r, string text)
    {
        CTBWin win = null;
        if (!winList.TryGetValue(id, out win))
        {
            win = new CTBWin(id, r, text, text);
            winList.Add(id, win);
        }

        if (HighLogic.LoadedSceneIsEditor)
            win.PreventEditorClickthrough();
        if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneHasPlanetarium)
            win.PreventInFlightClickthrough();
        win.rect = r;
        return win;
    }

    public static CTBWin ClickThruBlockerWindow(int id, Rect screenRect, GUI.WindowFunction func, string text, GUIStyle style, params GUILayoutOption[] options)
    {
        Rect r = GUILayout.Window(id, screenRect, func, text, style, options);

        return UpdateList(id, r, text);
    }

    public static CTBWin ClickThruBlockerWindow(int id, Rect screenRect, GUI.WindowFunction func, string text, params GUILayoutOption[] options)
    {
        Rect r = GUILayout.Window(id, screenRect, func, text, options);

        return UpdateList(id, r, text);
    }

    public static CTBWin ClickThruBlockerWindow(int id, Rect screenRect, GUI.WindowFunction func, GUIContent content, params GUILayoutOption[] options)
    {
        Rect r = GUILayout.Window(id, screenRect, func, content, options);

        return UpdateList(id, r, id.ToString());
    }

    public static CTBWin ClickThruBlockerWindow(int id, Rect screenRect, GUI.WindowFunction func, Texture image, params GUILayoutOption[] options)
    {
        Rect r = GUILayout.Window(id, screenRect, func, image, options);

        return UpdateList(id, r, id.ToString());
    }



    internal static class Log
    {
        internal static void Info(string s)
        {
            Debug.Log(s);
        }
    }





}

#endif