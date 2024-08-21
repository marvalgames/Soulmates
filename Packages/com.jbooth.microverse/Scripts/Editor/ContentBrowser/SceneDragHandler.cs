using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using static JBooth.MicroVerseCore.Browser.ContentBrowser;

#if __MICROVERSE_ROADS__
using UnityEngine.Splines;
#endif

namespace JBooth.MicroVerseCore.Browser
{
    public class SceneDragHandler
    {
        /// <summary>
        /// Identifier for the generic data of a drag and drop operation
        /// </summary>
        public static string EnabledCollidersId = "Enabled Colliders";

        /// <summary>
        /// Identifier whether the draggable is a MicroVerse object or not.
        /// Had to be introduced in case the content browser remained open the entire session.
        /// Otherwise it would handle all objects that are dragged into the scene and eg remove them after the drop
        /// </summary>
        public static string IsMicroverseDraggableId = "MicroVerse Draggable";

        /// <summary>
        /// Whether shift was pressed during the start of the drag operation or not
        /// </summary>
        public static string WasShiftPressed = "Shift Pressed";

        /// <summary>
        /// Whether control was pressed during the start of the drag operation or not
        /// </summary>
        public static string WasControlPressed = "Control Pressed";

        private ContentBrowser browser;

        public SceneDragHandler(ContentBrowser browser)
        {
            this.browser = browser;
        }

        public void OnEnable()
        {
            SceneView.beforeSceneGui += this.OnSceneGUI;
        }

        public void OnDisable()
        {
            SceneView.beforeSceneGui -= this.OnSceneGUI;
        }

        private void OnSceneGUI(SceneView obj)
        {
            HandleDragAndDropEvents();
        }

        public void OnDragStart( PresetItem preset)
        {
            if (MicroVerse.instance == null)
                return;
            // flag that the height stamp is being added
            // signals microverse that it shouldn't sync the heightmap
            // otherwise the raycast would cast against this new stamp and we'd get flicker
            MicroVerse.instance.IsAddingHeightStamp = browser.GetSelectedTab() == Tab.Height;

            DragAndDrop.PrepareStartDrag();
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            DragAndDrop.SetGenericData("preset", preset);
            DragAndDrop.SetGenericData(IsMicroverseDraggableId, true);

            // record if shift was pressed at the start of the drag operation
            bool wasShiftPressed = Event.current.shift;
            DragAndDrop.SetGenericData(WasShiftPressed, wasShiftPressed);

            // record if control was pressed at the start of the drag operation
            bool wasControlPressed = Event.current.control;
            DragAndDrop.SetGenericData(WasControlPressed, wasControlPressed);

            DragAndDrop.StartDrag("Dragging MyData");

        }

        public void HandleDragAndDropEvents()
        {
            if (Event.current.type != EventType.DragUpdated && Event.current.type != EventType.DragPerform)
                return;

            if (EditorWindow.mouseOverWindow == browser)
                return;

            object isMicroVerseDraggable = DragAndDrop.GetGenericData( IsMicroverseDraggableId);

            bool valid = isMicroVerseDraggable != null && isMicroVerseDraggable is bool && (bool)isMicroVerseDraggable == true;

            if (!valid)
                return;

            object wasShiftPressedObject = DragAndDrop.GetGenericData(WasShiftPressed);
            object wasControlPressedObject = DragAndDrop.GetGenericData(WasControlPressed);

            bool wasShiftPressed = wasShiftPressedObject != null && wasShiftPressedObject is bool && (bool)wasShiftPressedObject == true;
            bool wasControlPressed = wasControlPressedObject != null && wasControlPressedObject is bool && (bool)wasControlPressedObject == true;

            PresetItem preset = (PresetItem)DragAndDrop.GetGenericData("preset");
            if (preset != null)
            {
                DragAndDrop.SetGenericData("preset", null);

                GameObject draggable = browser.contentTabs[(int)browser.GetSelectedTab()].Spawn(browser, preset, wasShiftPressed);

                DragAndDrop.objectReferences = new GameObject[] { draggable };
                DragAndDrop.paths = null;

                // disable colliders, we don't want to raycast against self; store as generic data for re-enabling later
                Collider[] enabledColliders = draggable.GetComponentsInChildren<Collider>().Where(x => x.enabled == true).ToArray();
                foreach (Collider collider in enabledColliders)
                {
                    collider.enabled = false;
                }
                DragAndDrop.SetGenericData( EnabledCollidersId, enabledColliders);

            }


            if (Event.current.type == EventType.DragUpdated)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
                Vector3 point;

                if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
                {
                    // if( !instance.activeInHierarchy)
                    //    instance.SetActive(true);

                    point = hit.point;
                }
                else
                {
                    point = ray.origin + ray.direction * 120;
                    if (point.y < 0 && ray.origin.y > 0)
                    {
                        // if we are above 0, lets clamp the ray at the world axis
                        Plane plane = new Plane(Vector3.up, Vector3.zero);
                        float distance = 0;
                        if (plane.Raycast(ray, out distance))
                        {
                            // get the hit point:
                            point = ray.GetPoint(distance);
                        }
                    }
                }

                if (DragAndDrop.objectReferences.Length == 1)
                {
                    if (DragAndDrop.objectReferences[0] is GameObject)
                    {
                        GameObject go = DragAndDrop.objectReferences[0] as GameObject;
                        if (browser.GetSelectedTab() == Tab.Height)
                        {
                            point.y = 0; // height stamps always at 0
                        }
                        else if (browser.GetSelectedTab() == Tab.Roads || browser.GetSelectedTab() == Tab.Caves)
                        {
#if __MICROVERSE_ROADS__
                            point.y += browser.roadHeightOffset;
#endif
                        }
                        go.transform.position = point;
                    }
                }
            }
            else if (Event.current.type == EventType.DragPerform)
            {

                DragAndDrop.SetGenericData(IsMicroverseDraggableId, false);
                DragAndDrop.SetGenericData(WasShiftPressed, false);
                DragAndDrop.SetGenericData(WasControlPressed, false);

                DragAndDrop.AcceptDrag();

                if (DragAndDrop.objectReferences.Length == 1)
                {
                    // re-enable colliders which were diabled before the drag operation
                    Collider[] enabledColliders = (Collider[])DragAndDrop.GetGenericData(EnabledCollidersId);
                    if (enabledColliders != null)
                    {
                        foreach (Collider collider in enabledColliders)
                        {
                            collider.enabled = true;
                        }
                    }

                    // reference to new object
                    GameObject go = DragAndDrop.objectReferences[0] as GameObject;

                    bool centerTerrain = wasControlPressed;
                    if (centerTerrain)
                    {
                        Terrain[] terrains = MicroVerse.instance.GetComponentsInChildren<Terrain>();

                        Bounds worldBounds = TerrainUtil.ComputeTerrainBounds(terrains);

                        // position
                        float y = worldBounds.center.y - worldBounds.size.y / 2f;

                        if (browser.GetSelectedTab() == Tab.Roads || browser.GetSelectedTab() == Tab.Caves)
                        {
#if __MICROVERSE_ROADS__
                            y += browser.roadHeightOffset;
#endif
                        }

                        go.transform.transform.position = new Vector3(worldBounds.center.x, y, worldBounds.center.z);
                    }

                    // select new object
                    Selection.activeObject = go;
                }

                // cleanup drag
                DragFinished();

                DragAndDrop.objectReferences = new Object[0];
                DragAndDrop.visualMode = DragAndDropVisualMode.None;
                DragAndDrop.SetGenericData(EnabledCollidersId, null);

                Event.current.Use();

            }
            /* note: doesn't seem to work, DragExited is also invoked after DragPerform
            // eg escape pressed
            else if (Event.current.type == EventType.DragExited)
            {
                if (DragAndDrop.objectReferences.Length == 1)
                {
                    GameObject go = DragAndDrop.objectReferences[0] as GameObject;
                    GameObject.DestroyImmediate(go);
                }

                // cleanup drag
                DragAndDrop.objectReferences = new Object[0];
                DragAndDrop.visualMode = DragAndDropVisualMode.None;

                Event.current.Use();

                DragFinished();
            }
            */
        }


        private void DragFinished()
        {
            GameObject go = DragAndDrop.objectReferences[0] as GameObject;

#if __MICROVERSE_ROADS__
            if (go != null)
            {
                SplineRelativeTransform srt = go.GetComponent<SplineRelativeTransform>();
                if (srt != null)
                {
                    RoadSystem[] rss = GameObject.FindObjectsOfType<RoadSystem>();
                    if (rss != null)
                    {
                        RoadSystem roadSystem = null;
                        Road closest = null;
                        float closestDist = 9999999;
                        foreach (var rs in rss)
                        {
                            Road[] roads = rs.GetComponentsInChildren<Road>();
                            foreach (var road in roads)
                            {
                                if (road.splineContainer != null && road.splineContainer.Splines.Count > 0)
                                {
                                    var spline = road.splineContainer.Spline;
                                    var dist = SplineUtility.GetNearestPoint(spline, road.splineContainer.transform.worldToLocalMatrix.MultiplyPoint( go.transform.position), out _, out _);
                                    if (dist < closestDist)
                                    {
                                        closest = road;
                                        closestDist = dist;
                                        roadSystem = rs;
                                    }
                                }
                            }
                        }

                        if (closest != null && closestDist < 30)
                        {
                            srt.splineContainer = closest.splineContainer;
                            srt.transform.SetParent(roadSystem.transform, true);
                            srt.CaptureOffset();
                            srt.enabled = true;
                        }
                    }
                }
            }
#endif
            // height stamp dragging finished => allow syncing again
            MicroVerse.instance.IsAddingHeightStamp = false;

            // perform action after drop for gameobjects that implement the interface
            #region IContentBrowserDropAction
            if( go != null)
            {
                PerformDropAction(go, out bool destroyGameObject);

                // optionally destroy the gameobject in case eg only data got applied
                if (destroyGameObject)
                {
                    GameObject.DestroyImmediate(go);
                }
            }
            #endregion IExecuteOnContentBrowserDrop

            if (go != null)
            {
                Undo.RegisterCreatedObjectUndo(go, go.name);
            }
            MicroVerse.instance?.Invalidate(null); // TODO : Do Better?
        }

        /// <summary>
        /// Execute actions in case the dropped gameobject implements them
        /// </summary>
        /// <param name="dropGo"></param>
        /// <param name="destroyGameObject"></param>
        private void PerformDropAction( GameObject dropGo, out bool destroyGameObject)
        {
            // find gameobjects that implement the interface
            IContentBrowserDropAction[] dropActions = dropGo.GetComponentsInChildren<IContentBrowserDropAction>();

            destroyGameObject = false;

            // execute the actions, find out if gameobject should be destroyed
            for (int i = 0; i < dropActions.Length; i++)
            {
                IContentBrowserDropAction dropAction = dropActions[i];

                if (dropAction == null)
                    continue;

                // perform action
                dropAction.Execute(out bool destroyAfterExecute);

                // collect information if all instances require to destroy the gameobject; if there's at least 1 that doesn't want it destroyed, then it won't be
                destroyGameObject = i == 0 ? destroyAfterExecute : destroyGameObject && destroyAfterExecute;
            }
        }
    }
}