using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;



[CustomEditor(typeof(sr_Order))]
public class sr_OrderEditor : Editor
{

    public SerializedProperty
        _type,
        _duration,
        _nextOrder,
        _parallelOrders,

        _gizmo,
        _textline,
        _audiofile,
        _characterAnimator,
        _animatorState,
        _animationComponent,
        _animationClip,
        _astarAgent,
        _prefab,
        _ingameObject,
        _methodToCall,
        _timer,
        _boolean,
        _boolean2;




    private void OnEnable()
    {
        //Setup the SerializedProperties
        _type = serializedObject.FindProperty("type");
        _duration = serializedObject.FindProperty("duration");
        _nextOrder = serializedObject.FindProperty("nextOrder");
        _parallelOrders = serializedObject.FindProperty("parallelOrders");

        _gizmo = serializedObject.FindProperty("gizmo");
        _textline = serializedObject.FindProperty("textline");
        _audiofile = serializedObject.FindProperty("audiofile");
        _characterAnimator = serializedObject.FindProperty("characterAnimator");
        _animatorState = serializedObject.FindProperty("animatorState");
        _animationComponent = serializedObject.FindProperty("animationComponent");
        _animationClip = serializedObject.FindProperty("animationClip");
        _astarAgent = serializedObject.FindProperty("astarAgent");
        _prefab = serializedObject.FindProperty("prefab");
        _ingameObject = serializedObject.FindProperty("ingameObject");
        _methodToCall = serializedObject.FindProperty("methodToCall");
        _timer = serializedObject.FindProperty("timer");
        _boolean = serializedObject.FindProperty("boolean");
        _boolean2 = serializedObject.FindProperty("boolean2");
    }



    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(_type);
        sr_Order.Type typ = (sr_Order.Type)_type.enumValueIndex;


        switch (typ) {
            case sr_Order.Type.SetControl:
                EditorGUILayout.PropertyField(_boolean, new GUIContent("True"));
                EditorGUILayout.PropertyField(_boolean2, new GUIContent("Restore Last Gameplay Rotation"));
                //EditorGUILayout.TextArea ( _lines, new GUIContent("Lines"));
                //EditorGUILayout.IntSlider ( valForA_Prop, 0, 10, new GUIContent("valForA") );
                //EditorGUILayout.IntSlider ( valForAB_Prop, 0, 100, new GUIContent("valForAB") );
                break;

            case sr_Order.Type.SetCam:
                EditorGUILayout.PropertyField(_gizmo, new GUIContent("Camera Gizmo"));
                //EditorGUI.PropertyField(new Rect(5, 5, 60, EditorGUIUtility.singleLineHeight), _duration, GUIContent.none);
                //EditorGUILayout.FloatField (_duration, new GUIContent("duration"));

                //EditorGUILayout.PropertyField ( controllable_Prop, new GUIContent("controllable") );    
                //EditorGUILayout.IntSlider ( valForAB_Prop, 0, 100, new GUIContent("valForAB") );
                break;

            case sr_Order.Type.Traveling:
                EditorGUILayout.PropertyField(_gizmo, new GUIContent("Camera Gizmo Target"));
                EditorGUILayout.Slider(_timer, 0, 10, new GUIContent("Timer"));
                break;

            case sr_Order.Type.Text:
                //longStringProp.stringValue = EditorGUILayout.TextArea(longStringProp.stringValue, GUILayout.MaxHeight(75));
                _textline.stringValue = EditorGUILayout.TextArea(_textline.stringValue, GUILayout.MaxHeight(75));
                break;

            case sr_Order.Type.Audio:
                EditorGUILayout.PropertyField(_audiofile, new GUIContent("Audio Clip"));
                break;

            case sr_Order.Type.Animator:
                EditorGUILayout.PropertyField(_ingameObject, new GUIContent("Character Animator"));
                EditorGUILayout.PropertyField(_animatorState, new GUIContent("State Name"));
                EditorGUILayout.PropertyField(_timer, new GUIContent("Crossfade Time"));
                break;

            case sr_Order.Type.Animation:
                EditorGUILayout.PropertyField(_animationComponent, new GUIContent("Animation Component"));
                EditorGUILayout.PropertyField(_animationClip, new GUIContent("Clip Name"));
                break;

            case sr_Order.Type.Astar:
                EditorGUILayout.PropertyField(_ingameObject, new GUIContent("Agent"));
                EditorGUILayout.PropertyField(_gizmo, new GUIContent("Target"));
                EditorGUILayout.PropertyField(_boolean, new GUIContent("Run"));
                break;

            case sr_Order.Type.Instantiate:
                EditorGUILayout.PropertyField(_prefab, new GUIContent("Prefab"));
                EditorGUILayout.PropertyField(_gizmo, new GUIContent("Position/Rotation Gizmo"));
                break;

            case sr_Order.Type.Lerp:
                EditorGUILayout.PropertyField(_ingameObject, new GUIContent("Object"));
                EditorGUILayout.PropertyField(_gizmo, new GUIContent("Target"));
                EditorGUILayout.PropertyField(_timer, new GUIContent("Timer"));
                break;

            case sr_Order.Type.Event:
                EditorGUILayout.PropertyField(_ingameObject, new GUIContent("Object"));
                EditorGUILayout.PropertyField(_methodToCall, new GUIContent("Method To Call"));
                break;


            case sr_Order.Type.Teleport:
                EditorGUILayout.PropertyField(_ingameObject, new GUIContent("Object"));
                EditorGUILayout.PropertyField(_gizmo, new GUIContent("Position/Rotation Gizmo"));
                break;

            case sr_Order.Type.EnableDisable:
                EditorGUILayout.PropertyField(_ingameObject, new GUIContent("Object"));
                EditorGUILayout.PropertyField(_boolean, new GUIContent("Enable"));
                break;

            case sr_Order.Type.Wait:
                break;

        }

        EditorGUILayout.PropertyField(_duration, new GUIContent("Duration"));

        EditorGUILayout.PropertyField(_nextOrder, new GUIContent("Next Order"));
        EditorGUILayout.PropertyField(_parallelOrders, new GUIContent("Parallel Orders"), true);

        serializedObject.ApplyModifiedProperties();
    }
}

