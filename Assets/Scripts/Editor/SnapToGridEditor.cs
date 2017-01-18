using UnityEngine;
using System.Collections;
using UnityEditor;
using EditorSupport;

[CustomEditor(typeof(SnapToGrid))]
public class SnapToGridEditor : Editor
{
    SnapToGrid m_myTarget;
    float distance;
    Vector3 gridPos = new Vector3();
    GameObject onMouseOverGameObject;
    bool m_instantiated = false;
    static private bool m_controlPressed = false;
    static private bool m_rotationKeyPressed = false;
    static private bool m_shiftPressed = false;
    static private bool m_leftMousePressed = false;
    //private bool m_showGridKeyPressed = false; 
    static bool isThisObject = false;
    static bool isMouseDown = false;
    private bool objectDragged = false;
    //GameObject m_instantiatedGameObject = new GameObject(); 
    private int rowDecal = 0;
    private int colDecal = 0;
    private int finalCol = 0;
    private int finalRow = 0;

    private void OnEnable()
    {
        m_instantiated = false;
        SceneView.onSceneGUIDelegate += EventHandler;
    }

    private void OnDisable()
    {
        m_instantiated = false;
        SceneView.onSceneGUIDelegate -= EventHandler;
    }

    private void EventHandler(SceneView sceneview)
    {
        if (LevelGrid.Ins == null)
            return;

        if (m_myTarget == null)
            m_myTarget = target as SnapToGrid;

        //we should call this only when changing values, not always... but then again do it later. 
        ToolsSupport.UnityHandlesHidden = LevelGrid.Ins.hideUnityHandles;
        if (!LevelGrid.Ins.hideUnityHandles && LevelGrid.Ins.autoRectTool)
            Tools.current = Tool.Rect;

        if (m_myTarget.useChildBoxCollider != null)
        {
            BoxCollider _boxCollider = m_myTarget.GetComponent<BoxCollider>();
            _boxCollider.size = m_myTarget.useChildBoxCollider.bounds.size;
            _boxCollider.center = m_myTarget.useChildBoxCollider.bounds.center;

        }


        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        Ray worldRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

        RaycastHit[] hits = Physics.RaycastAll(worldRay, 1000);

        for (int i = 0; i < hits.Length; i++)
        {

            //Debug.Log("hit:" + hits[i].collider.gameObject.name);
            if (hits[i].transform.gameObject == LevelGrid.Ins.gameObject)
            {
                gridPos = hits[i].point;
            }
            else if (hits[i].transform.GetComponent<SnapToGrid>() != null)
            {
                //Debug.Log("Object name: " + hits[i].collider.name);
                onMouseOverGameObject = hits[i].transform.gameObject;
            }
        }

        if (hits.Length <= 0)
            onMouseOverGameObject = null;

        //Debug.Log(onMouseOverGameObject);

        UpdateKeyEvents();

        //mouse position in the grid
        //recalculate this with new values. 
        float col = (float)gridPos.x / ((float)LevelGrid.Ins.gridSize * LevelGrid.Ins.scaleFactor);
        float row = (float)gridPos.z / ((float)LevelGrid.Ins.gridSize * LevelGrid.Ins.scaleFactor);

        LevelGrid.Ins.UpdateInputGridHeight();


        if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
        {

            isMouseDown = true;
            if (onMouseOverGameObject == null)
                return;

            if (onMouseOverGameObject == m_myTarget.gameObject)
            {
                isThisObject = true;
            }
            else
            {
                isThisObject = false;
                Selection.activeGameObject = (onMouseOverGameObject != null) ? onMouseOverGameObject : null;
            }

        }

        if (isMouseDown && m_rotationKeyPressed)
        {
            if (LevelGrid.Ins.selectedGameObject == null)
                LevelGrid.Ins.selectedGameObject = Selection.activeGameObject;

            LevelGrid.Ins.selectedGameObject.transform.eulerAngles += new Vector3(0, 90f, 0);
            CalculateRotation(col, row);
            m_rotationKeyPressed = false;
        }


        //mouse click and dragandrop
        if (Event.current.type == EventType.MouseDrag && Event.current.button == 0)
        {
            if (Selection.activeGameObject == null)
                return;

            CalculateRotation(col, row);
            SnapToGrid(finalCol, finalRow, LevelGrid.Ins.height);

            objectDragged = true;
        }

        //if mouse released when control pressed, make a copy / otherwise, destroy old object. 
        if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
        {
            isMouseDown = false;
            if (LevelGrid.Ins.selectedGameObject)
            {
                //make copy if control is pressed
                if (!m_controlPressed)
                {
                    Undo.IncrementCurrentGroup();
                    if (m_instantiated)
                        Undo.DestroyObjectImmediate(m_myTarget.gameObject);
                }
                if (objectDragged)
                {
                    Selection.activeGameObject = LevelGrid.Ins.selectedGameObject;
                    objectDragged = false;
                }
                m_instantiated = false;
            }
        }

        //show hide grid
        if ((Event.current.type == EventType.keyUp) && (Event.current.keyCode == KeyCode.O))
        {
            LevelGrid.Ins.showGrid = !LevelGrid.Ins.showGrid;
        }
    }

    private void UpdateKeyEvents()
    {
        if ((Event.current.type == EventType.keyDown) && (Event.current.keyCode == KeyCode.A || Event.current.keyCode == KeyCode.S))
            m_rotationKeyPressed = true;

        if ((Event.current.type == EventType.keyUp) && (Event.current.keyCode == KeyCode.A || Event.current.keyCode == KeyCode.S))
            m_rotationKeyPressed = false;

        //check if control is pressed. 
        if ((Event.current.type == EventType.keyDown) && (Event.current.keyCode == KeyCode.LeftControl || Event.current.keyCode == KeyCode.RightControl))
            m_controlPressed = true;

        if ((Event.current.type == EventType.keyUp) && (Event.current.keyCode == KeyCode.LeftControl || Event.current.keyCode == KeyCode.RightControl))
            m_controlPressed = false;

        //if ((Event.current.type == EventType.keyDown) && (Event.current.keyCode == KeyCode.I ))
        //    m_showGridKeyPressed = true;

        //if ((Event.current.type == EventType.keyUp) && (Event.current.keyCode == KeyCode.I ))
        //    m_showGridKeyPressed = false;


    }

    private void CalculateRotation(float col, float row)
    {
        if (LevelGrid.Ins.selectedGameObject == null)
            LevelGrid.Ins.selectedGameObject = Selection.activeGameObject;
        //0
        if (LevelGrid.Ins.selectedGameObject.transform.eulerAngles.y > -2 && LevelGrid.Ins.selectedGameObject.transform.eulerAngles.y < 2)
        {
            colDecal = 0;
            rowDecal = 0;
            finalCol = (Mathf.Sign(col) < 0) ? (int)col - 1 + colDecal : (int)col + colDecal;
            finalRow = (Mathf.Sign(row) < 0) ? (int)row - 1 + rowDecal : (int)row + rowDecal;
            SnapToGrid(finalCol, finalRow, LevelGrid.Ins.height);
        }
        //90
        else if (LevelGrid.Ins.selectedGameObject.transform.eulerAngles.y > 88 && LevelGrid.Ins.selectedGameObject.transform.eulerAngles.y < 92)
        {
            colDecal = 0;
            rowDecal = 1;
            finalCol = (Mathf.Sign(col) < 0) ? (int)col - 1 + colDecal : (int)col + colDecal;
            finalRow = (Mathf.Sign(row) < 0) ? (int)row - 1 + rowDecal : (int)row + rowDecal;
            SnapToGrid(finalCol, finalRow, LevelGrid.Ins.height);
        }
        //180
        else if (LevelGrid.Ins.selectedGameObject.transform.eulerAngles.y > 178 && LevelGrid.Ins.selectedGameObject.transform.eulerAngles.y < 182)
        {
            colDecal = 1;
            rowDecal = 1;
            finalCol = (Mathf.Sign(col) < 0) ? (int)col - 1 + colDecal : (int)col + colDecal;
            finalRow = (Mathf.Sign(row) < 0) ? (int)row - 1 + rowDecal : (int)row + rowDecal;
            SnapToGrid(finalCol, finalRow, LevelGrid.Ins.height);
        }
        //270
        else if (LevelGrid.Ins.selectedGameObject.transform.eulerAngles.y > 268 && LevelGrid.Ins.selectedGameObject.transform.eulerAngles.y < 272)
        {
            colDecal = 1;
            rowDecal = 0;
            finalCol = (Mathf.Sign(col) < 0) ? (int)col - 1 + colDecal : (int)col + colDecal;
            finalRow = (Mathf.Sign(row) < 0) ? (int)row - 1 + rowDecal : (int)row + rowDecal;
            SnapToGrid(finalCol, finalRow, LevelGrid.Ins.height);
        }
    }

    private void SnapToGrid(int col, int row, float height)
    {
        if (m_myTarget == null)
            m_myTarget = target as SnapToGrid;

        if (!LevelGrid.Ins.snapToGrid)
            return;

        GameObject obj = m_myTarget.gameObject;
        if (!m_instantiated)
        {

            if (PrefabUtility.GetPrefabParent(Selection.activeObject) != null)
            {
                obj = PrefabUtility.InstantiatePrefab(PrefabUtility.GetPrefabParent(Selection.activeObject) as GameObject) as GameObject;
                obj.transform.rotation = m_myTarget.gameObject.transform.rotation;

            }
            else
            {
                //Debug.Log("prefab parent not found");
                obj = Instantiate(m_myTarget.gameObject);
            }

            obj.name = m_myTarget.gameObject.name;
            if (LevelGrid.Ins.parentGameObject != null) obj.transform.parent = LevelGrid.Ins.parentGameObject;

            LevelGrid.Ins.selectedGameObject = obj;

            m_instantiated = true;
            Undo.RegisterCreatedObjectUndo(obj, "Create " + obj.name);
        }
        LevelGrid.Ins.selectedGameObject.transform.position = LevelGrid.Ins.GridToWorldCoordinates(col, row, height);
        Undo.IncrementCurrentGroup();
    }
}
