using UnityEngine;
using System.Collections;
using UnityEditor;
using EditorSupport;

[CustomEditor(typeof(LevelGrid))]
public class LevelGridEditor : Editor
{
    LevelGrid _myTarget;

    private void OnEnable()
    {
        _myTarget = target as LevelGrid;
        _myTarget.boxCollider =   _myTarget.GetComponent<BoxCollider>();
        SceneView.onSceneGUIDelegate += EventHandler;
    }

    private void OnDisable()
    {
        SceneView.onSceneGUIDelegate -= EventHandler;
    }

    private void EventHandler(SceneView sceneview)
    {
        if (!_myTarget)
            _myTarget = target as LevelGrid;

        ToolsSupport.UnityHandlesHidden = LevelGrid.Ins.hideUnityHandles;
        _myTarget.transform.position = Vector3.zero;

        float cols = _myTarget.sizeColums;
        float rows = _myTarget.sizeRows;

        //properly place the collider
       _myTarget.boxCollider = _myTarget.UpdateBoxCollider(_myTarget.boxCollider, cols, rows, _myTarget.height);
        LevelGrid.Ins.UpdateInputGridHeight();
    }



    override public void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("Open Grid Window", GUILayout.Width(255)))
        {
            OpenLevelGridWindow();
        }
    }


    [MenuItem("Level Grid/Show Level Grid Window %g", false, 1)]
    static public void OpenLevelGridWindow()
    {
        LevelGridWindow window = (LevelGridWindow)EditorWindow.GetWindow(typeof(LevelGridWindow), false, "Grid Editor");
        window.Init();


    }

    [MenuItem("Level Grid/Add LevelGrid %#g", false, 2)]
    [MenuItem("GameObject/Level Grid", false, 6)]
    static public void AddLevelGrid()
    {
        if (LevelGrid.Ins == null)
        {
            GameObject go = Instantiate(Resources.Load("LevelGrid", typeof(GameObject))) as GameObject;
            go.transform.position = Vector3.zero;
            LevelGrid.Ins = go.GetComponent<LevelGrid>();
        } else
        {
            Debug.LogError("Already a LevelGrid Singleton in this scene");
        }
    }

    [MenuItem("GameObject/3D Object/Snap To Grid GameObject")]
    [MenuItem("Level Grid/Add SnapToGrid GameObject", false, 3)]
    public static void CreateObject()
    {
        //GameObject gob = Instantiate(Resources.Load("Standard SnapToGrid", typeof(GameObject))) as GameObject;
        //GameObject go = PrefabUtility.InstantiatePrefab(PrefabUtility.GetPrefabParent(Resources.Load("Standard SnapToGrid", typeof(GameObject)))) as GameObject;
        GameObject go = PrefabUtility.InstantiatePrefab(Resources.Load("Standard SnapToGrid")) as GameObject; 
        go.transform.position = Vector3.zero;
        go.name = "SnapToGrid";
    }

}
