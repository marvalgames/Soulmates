#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using FIMSpace.FEditor;

namespace FIMSpace.Generating
{
    public class SimpleGUI3DView : UnityEditor.Editor
    {
        private PreviewRenderUtility _previewRenderUtility;

        public PreviewRenderUtility Render { get { return _previewRenderUtility; } }


        [NonSerialized] public bool UseScroll = true;


        #region Editor Window Setup

        public override bool HasPreviewGUI()
        {
            ValidateData();
            return true;
        }

        public override bool RequiresConstantRepaint()
        {
            return true;
        }


        void OnDestroy()
        {
            if (_previewRenderUtility != null) _previewRenderUtility.Cleanup();
        }

        private void OnDisable()
        {
            if (_previewRenderUtility != null) _previewRenderUtility.Cleanup();
        }


        #endregion


        public static Material UnityDefaultDiffuseMaterial { get { if (_unityDefaultMat == null) _unityDefaultMat = new Material(Shader.Find("Diffuse")); return _unityDefaultMat; } }
        private static Material _unityDefaultMat = null;



        private void ValidateData()
        {
            if (_previewRenderUtility == null)
            {
                _previewRenderUtility = new PreviewRenderUtility();
                _previewRenderUtility.camera.orthographic = false;
                _previewRenderUtility.camera.fieldOfView = 50f;
                _previewRenderUtility.camera.nearClipPlane = 0.001f;
                _previewRenderUtility.camera.farClipPlane = 1000f;
                _previewRenderUtility.lights[0].transform.rotation *= Quaternion.Euler(45f, 165f, 0f);
                _previewRenderUtility.lights[0].shadows = LightShadows.None;
                _previewRenderUtility.ambientColor = new Color(0.4f, 0.35f, 0.2f);
                //_previewRenderUtility.lights[0].shadowStrength = 1f;
                _previewRenderUtility.camera.transform.position = new Vector3(0, 1, -7);
                _previewRenderUtility.camera.transform.rotation = Quaternion.Euler(-12, 155, 0);
            }
        }

        //Vector2 cameraOffset = new Vector2(0, 0);
        Vector2 sphericCamRot = new Vector2(28, -72);
        float camDistance = 6.75f;

        public override void OnPreviewGUI(Rect r, GUIStyle background)
        {
            #region Refresh

            if (_previewRenderUtility == null)
            {
                ValidateData();
                return;
            }

            #endregion

            camDistance = Mathf.Clamp(camDistance, 0.5f, 10f);
            sphericCamRot.x = Mathf.Clamp(sphericCamRot.x, -60f, 60f);

            Quaternion camRot = Quaternion.Euler(sphericCamRot);
            Vector3 newPos = camRot * new Vector3(0f, 0f, -camDistance);

            _previewRenderUtility.camera.transform.position = newPos;
            _previewRenderUtility.camera.transform.rotation = camRot;

            if (Event.current.type == EventType.Repaint)
            {
                _previewRenderUtility.BeginPreview(r, background);
                

                if (RenderAction != null) RenderAction.Invoke();


                Handles.SetCamera(_previewRenderUtility.camera);

                Handles.color = Color.gray * 0.7f;
                Handles.DrawLine(new Vector3(-1.5f, 0, -5), new Vector3(-1.5f, 0, 5));
                Handles.DrawLine(new Vector3(1.5f, 0, -5), new Vector3(1.5f, 0, 5));

                Handles.DrawLine(new Vector3(-5, 0, 1.5f), new Vector3(5, 0, 1.5f));
                Handles.DrawLine(new Vector3(-5, 0, -1.5f), new Vector3(5, 0, -1.5f));


                //Handles.color = Color.gray * 0.6f;
                //float off = 3;
                //Handles.DrawLine(new Vector3(off, 5, off), new Vector3(off, -5, off));
                //Handles.DrawLine(new Vector3(-off, 5, -off), new Vector3(-off, -5, -off));

                //Handles.DrawLine(new Vector3(-off, 5, off), new Vector3(-off, -5, off));
                //Handles.DrawLine(new Vector3(off, 5, -off), new Vector3(off, -5, -off));


                Handles.color = new Color(0.75f, 1f, 0.75f, 0.5f);
                Handles.SphereHandleCap(0, Vector2.zero, Quaternion.identity, 0.4f, EventType.Repaint);

                Handles.color = new Color(0.4f, 0.3f, 1f, 1f);
                FGUI_Handles.DrawArrow(new Vector3(0f, 0, 1.5f), Quaternion.identity, 1.5f, 3f, 1f);

                Handles.color = Color.white;


                if (HandlesAction != null) HandlesAction.Invoke();


                _previewRenderUtility.Render();

                Texture resultRender = _previewRenderUtility.EndPreview();

                GUI.color = BackgroundColorize;
                GUI.DrawTexture(r, resultRender, ScaleMode.StretchToFill, false);
                GUI.color = Color.white;
            }

            if (UpdateInput)
            {
                InputExecuteCustomQueue(r);
            }
        }

        public System.Action RenderAction = null;
        public System.Action HandlesAction = null;

        public bool UpdateInput = true;
        public Color BackgroundColorize = Color.white;

        public void InputExecuteCustomQueue(Rect r)
        {

            bool mouseContained = r.Contains(Event.current.mousePosition);

            if (mouseContained)
            {
                if (UseScroll)
                {

                    if (Event.current.type == EventType.ScrollWheel)
                    {
                        if (Event.current.delta.y > 0)
                            camDistance += 0.5f;
                        else
                        if (Event.current.delta.y < 0)
                            camDistance -= 0.5f;

                        Event.current.Use();
                    }
                }

            }

            if (Event.current.type == EventType.MouseDrag)
            {
                if (Event.current.button != 2)
                {
                    sphericCamRot.x += Event.current.delta.y;
                    sphericCamRot.y += Event.current.delta.x;
                }
                //else
                //{
                //    cameraOffset.x -= Event.current.delta.x * 0.02f * camDistance;
                //    cameraOffset.y += Event.current.delta.y * 0.02f * camDistance;
                //}

                Event.current.Use();
            }

        }
    }
}

#endif