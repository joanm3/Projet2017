using UnityEngine;
using UnityEditor;
using System.Collections;
using EditorSupport;

public class LevelGridWindow : EditorWindow
{
    LevelGrid m_levelGrid = null;
    SnapToGrid m_snapToGrid = null;
    static bool _showControls = true;
    static bool _selectionWithoutSnap = false;
    static bool _transformSelected = false;


    private static Texture2D tex;


    public void Init()
    {
        m_levelGrid = LevelGrid.Ins;
    }

    private void OnEnable()
    {
        _selectionWithoutSnap = false;
        tex = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        tex.SetPixel(0, 0, new Color(0.8f, 0.8f, 0.75f));
        tex.Apply();
        SceneView.onSceneGUIDelegate += EventHandler;
    }

    private void OnDisable()
    {
        SceneView.onSceneGUIDelegate -= EventHandler;
    }

    private void EventHandler(SceneView sceneview)
    {
        if (LevelGrid.Ins == null)
            return;

        _transformSelected = (Selection.activeTransform != null);

        if (Selection.activeTransform != null)
        {
            m_snapToGrid = Selection.activeTransform.GetComponent<SnapToGrid>();
        }

        if (Selection.activeTransform != null)
        {
            if (Selection.activeTransform.gameObject != LevelGrid.Ins.gameObject && m_snapToGrid == null)
            {
                _selectionWithoutSnap = true;
            }
            else
            {
                _selectionWithoutSnap = false;
            }
        }

        Repaint();


    }

    void OnGUI()
    {
        if (m_levelGrid == null)
        {
            Init();
        }

        GUI.DrawTexture(new Rect(0, 0, maxSize.x, maxSize.y), tex, ScaleMode.StretchToFill);
        GUIStyle MontStyle = new GUIStyle();



        MontStyle.alignment = TextAnchor.MiddleCenter;
        MontStyle.font = (Font)Resources.Load("HomeRem");
        MontStyle.fontSize = 40;
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Grid Editor", MontStyle);
        EditorGUILayout.Space();

        MontStyle.fontSize = 24;
        MontStyle.alignment = TextAnchor.LowerLeft;
        MontStyle.font = (Font)Resources.Load("CalligraphyFLF");
        EditorGUILayout.LabelField("Grid", MontStyle);

        if (LevelGrid.Ins == null)
        {
            EditorGUILayout.Space();
            if (GUILayout.Button("Add LevelGrid"))
            {
                LevelGridEditor.AddLevelGrid();
            }
            return;
        }

        EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(20f));
        m_levelGrid.snapToGrid = EditorGUILayout.Toggle(m_levelGrid.snapToGrid);
        EditorGUILayout.LabelField("Snap to grid");
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(20f));
        LevelGrid.Ins.hideUnityHandles = EditorGUILayout.Toggle(LevelGrid.Ins.hideUnityHandles);
        EditorGUILayout.LabelField("Hide Unity Handles");
        EditorGUILayout.EndHorizontal();

        if (!LevelGrid.Ins.hideUnityHandles)
        {
            EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(20f));
            LevelGrid.Ins.autoRectTool = EditorGUILayout.Toggle(LevelGrid.Ins.autoRectTool);
            EditorGUILayout.LabelField("Auto Rect Tool");
            EditorGUILayout.EndHorizontal();
        }


        EditorGUILayout.PrefixLabel("Grid Pow:");
        m_levelGrid.gridSize = (LevelGrid.Pow2)EditorGUILayout.EnumPopup(m_levelGrid.gridSize);
        EditorGUILayout.PrefixLabel("Height Change Pow:");
        m_levelGrid.heightGridSize = (LevelGrid.Pow2)EditorGUILayout.EnumPopup(m_levelGrid.heightGridSize);
        EditorGUILayout.Space();
        EditorGUILayout.Space();

        if (_transformSelected)
        {
            if (_selectionWithoutSnap)
            {

                EditorGUILayout.LabelField("Selected GameObject", MontStyle);
                if (GUILayout.Button("Snap GameObject to Grid"))
                {
                    Selection.activeTransform.gameObject.AddComponent<SnapToGrid>();
                    _selectionWithoutSnap = false;
                }

                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }

            if (m_snapToGrid != null)
            {
                EditorGUILayout.LabelField("Selected GameObject", MontStyle);

                EditorGUILayout.PrefixLabel("Pivot Point:");
                m_snapToGrid.pivot = (SnapToGrid.PivotPoint)EditorGUILayout.EnumPopup(m_snapToGrid.pivot);
                EditorGUILayout.BeginHorizontal(GUILayout.MaxWidth(20f));
                m_snapToGrid.removeBoxColliderInGame = EditorGUILayout.Toggle(m_snapToGrid.removeBoxColliderInGame);
                EditorGUILayout.LabelField("Remove Collider in game");
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.Space();
                EditorGUILayout.Space();
            }
        }


        _showControls = EditorGUILayout.Foldout(_showControls, "Controls");
        EditorGUILayout.Space();
        if (_showControls)
        {
            EditorGUILayout.HelpBox("Ctrl + move: copy", MessageType.None);
            EditorGUILayout.HelpBox("Y: move height up", MessageType.None);
            EditorGUILayout.HelpBox("U: move height down", MessageType.None);
            EditorGUILayout.HelpBox("I: reset height to zero", MessageType.None);
            EditorGUILayout.HelpBox("O: show / hide grid", MessageType.None);
            EditorGUILayout.HelpBox("A: rotate object 90º", MessageType.None);
        }


        if (LevelGrid.Ins != null)
        {
            if (Tools.current != Tool.Rect)
                EditorGUILayout.HelpBox("Use Rectbox for better Usage. Rectbox shortcut: T", MessageType.Info);
            if (LevelGrid.Ins.gameObject.layer != LayerMask.NameToLayer("Grid"))
                EditorGUILayout.HelpBox("Level Grid has to be in Layer with name 'Grid'", MessageType.Info);
        }


        //m_levelGrid.Update(); 
    }

}
