using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using ProjectGiants.Editor;

[CustomEditor(typeof(CharacterMotion))]
[CanEditMultipleObjects]
public class CharacterMotionEditor : Editor
{

    CustomProperties p = new CustomProperties();

    //character
    private readonly CProperty CHAR_STATE = new CProperty("characterState", "Character State");
    private readonly CProperty CHAR_MOV_TYPE = new CProperty("characterMovementType", "Movement Type");

    //references
    private readonly CProperty CAM = new CProperty("m_cam", "Camera");
    private readonly CProperty CHAR_RENDERER = new CProperty("m_characterRenderer", "Renderer");

    //movement
    private readonly CProperty START_RUNNING = new CProperty("m_startRunningSpeed", "Start Running Speed");
    private readonly CProperty MAX_ROT_SPEED = new CProperty("m_maxRotSpeed", "Max Rotation Speed");
    private readonly CProperty MIN_ROT_SPEED = new CProperty("m_minRotSpeed", "Min Rotation Speed");
    private readonly CProperty CURVE_ROT_SPEED = new CProperty("m_rotationBySpeed", "Rotation Speed Curve");


    //air
    private readonly CProperty AIR_GROUNDED = new CProperty("m_isGrounded", "Is Grounded?");
    private readonly CProperty AIR_GRAV_FORCE = new CProperty("m_airGravForce", "Air Gravity Force");
    private readonly CProperty AIR_CURVE = new CProperty("m_gravForceOverTime", "Air Gravity Curve");
    private readonly CProperty AIR_JUMP_FORCE = new CProperty("m_jumpForce", "Jump Force");
    private readonly CProperty AIR_GRAV_VECTOR = new CProperty("m_gravVector", "Gravity Vector");
    private readonly CProperty AIR_GRAV_INPUT = new CProperty("m_inputGravityMultiplier", "Air Stop Input Multiplier (not applied yet)");
    private readonly CProperty AIR_TIME_COOLDOWN = new CProperty("m_tJumpCooldown", "Time between jumps");


    //forces
    private readonly CProperty CHAR_MASS = new CProperty("massPlayer", "Character Mass");
    private readonly CProperty SURF_FRICTION = new CProperty("friction", "Surface Friction");
    private readonly CProperty CHAR_VELMAX = new CProperty("velMax", "Max Velocity (without surface forces)");
    private readonly CProperty CHAR_STOP_THRESHOLD = new CProperty("stopThreshold", "Stop Threshold");
    private readonly CProperty SURF_GLIDE = new CProperty("Glide", "Glide?");
    private readonly CProperty SURF_START_ANGLE = new CProperty("StartForcesAngle", "Start Forces Angle");
    private readonly CProperty SURF_FALL_ANGLE = new CProperty("FallInflectionAngle", "Fall Angle");


    //read only
    private readonly CProperty CHAR_DIRECTION = new CProperty("m_characterDirection", "Character Direction");
    private readonly CProperty CHAR_CURR_SPEED = new CProperty("m_characterCurrentSpeed", "Current Speed");
    private readonly CProperty SURF_GRAV_FORCE = new CProperty("m_gravForce", "Surface Grav Force");
    private readonly CProperty CHAR_MAX_FORCE = new CProperty("m_maxForce", "Max Force");
    private readonly CProperty INPUT_CURR_FORCE = new CProperty("m_inputCurrentForce", "Input Current Force");
    private readonly CProperty SUF_DESC_FORCE = new CProperty("m_surfaceCurrentDescentForce", "Surface Descent Force");
    private readonly CProperty TOTAL_FORCE = new CProperty("m_currentTotalForce", "TOTAL Force");
    private readonly CProperty SURFACE_ANGLE = new CProperty("m_surfaceAngle", "Surface Angle");

    private static bool _showCustomInspector = true;
    private static bool _showReferences;
    private static bool _showMovement;
    private static bool _showAir;
    private static bool _showForces;
    private static bool _showReadOnly;


    private void OnEnable()
    {
        p.RefreshProperites(serializedObject);
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        p.ClearTimingProperties();
        _showCustomInspector = EditorGUILayout.Toggle("Custom Inspector", _showCustomInspector);
        EditorGUILayout.Space();

        if (_showCustomInspector)
        {
            DrawCustomInspector();
        }
        else
        {
            base.DrawDefaultInspector();
        }

        serializedObject.ApplyModifiedProperties();
    }


    private void DrawCustomInspector()
    {
        p.DisplayField(CHAR_STATE);
        EditorGUILayout.Space();

        _showReferences = EditorGUILayout.Foldout(_showReferences, "References");
        if (_showReferences)
        {
            p.DisplayField(CAM);
            p.DisplayField(CHAR_RENDERER);
            EditorGUILayout.Space();
        }

        _showMovement = EditorGUILayout.Foldout(_showMovement, "Movement");
        if (_showMovement)
        {
            p.DisplayField(CHAR_MOV_TYPE);
            p.DisplayField(CURVE_ROT_SPEED);
            p.DisplayField(START_RUNNING);
            p.DisplayField(MAX_ROT_SPEED);
            p.DisplayField(MIN_ROT_SPEED);
            EditorGUILayout.Space();
        }


        _showAir = EditorGUILayout.Foldout(_showAir, "Air");
        if (_showAir)
        {
            p.DisplayField(AIR_GRAV_FORCE);
            p.DisplayField(AIR_CURVE);
            p.DisplayField(AIR_JUMP_FORCE);
            p.DisplayField(AIR_GRAV_VECTOR);
            p.DisplayField(AIR_GRAV_INPUT);
            p.DisplayField(AIR_TIME_COOLDOWN);

            EditorGUILayout.Space();
        }

        _showForces = EditorGUILayout.Foldout(_showForces, "Forces");
        if (_showForces)
        {
            p.DisplayField(CHAR_MASS);
            p.DisplayField(SURF_FRICTION);
            p.DisplayField(CHAR_VELMAX);
            p.DisplayField(CHAR_STOP_THRESHOLD);
            p.DisplayField(SURF_GLIDE);
            p.DisplayField(SURF_START_ANGLE);
            p.DisplayField(SURF_FALL_ANGLE);
            EditorGUILayout.Space();
        }

        _showReadOnly = EditorGUILayout.Foldout(_showReadOnly, "ReadOnly");
        if (_showReadOnly)
        {
            p.DisplayField(AIR_GROUNDED);
            p.DisplayField(CHAR_DIRECTION);
            p.DisplayField(CHAR_CURR_SPEED);
            p.DisplayField(SURF_GRAV_FORCE);
            p.DisplayField(CHAR_MAX_FORCE);
            p.DisplayField(SURFACE_ANGLE);
            p.DisplayField(INPUT_CURR_FORCE);
            p.DisplayField(SUF_DESC_FORCE);
            p.DisplayField(TOTAL_FORCE);
            EditorGUILayout.Space();
        }
    }


}
