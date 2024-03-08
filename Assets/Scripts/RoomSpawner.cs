using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public struct Item
{
    public GameObject obj;
    public Image preview;
    public string name;
}

public struct ItemP
{
    public SerializedProperty objP;
    public SerializedProperty previewP;
    public SerializedProperty nameP;
}

public class RoomSpawner : EditorWindow
{
    [MenuItem("Tools/Spawner")]
    public static void OpenGrid() => GetWindow<RoomSpawner>("Spawn rooms");

    //List<Item> rooms = new List<Item>();
    public Transform room;
    public Material previewMaterial;

    SerializedObject so;
    SerializedProperty roomP;

    GameObject[] prefabs;
    bool[] selectedPrefabs;

    private void OnEnable()
    {
        so = new SerializedObject(this);
        roomP = so.FindProperty("room");

        string[] guids = AssetDatabase.FindAssets("t:prefab", new[] { "Assets/Prefabs" });
        IEnumerable<string> paths = guids.Select(AssetDatabase.GUIDToAssetPath);
        prefabs = paths.Select(AssetDatabase.LoadAssetAtPath<GameObject>).ToArray();

        if (selectedPrefabs == null || selectedPrefabs.Length != prefabs.Length)
        {
            selectedPrefabs = new bool[prefabs.Length];
        }

        Selection.selectionChanged += Repaint;
        SceneView.duringSceneGui += DuringSceneGUI;
    }

    private void OnDisable()
    {
        Selection.selectionChanged -= Repaint;
        SceneView.duringSceneGui -= DuringSceneGUI;
    }

    private void OnGUI()
    {
        so.Update();

        GUIStyle st = GUI.skin.GetStyle("Label");
        st.alignment = TextAnchor.MiddleCenter;
        GUILayout.Label("Rooms", st);
        GUILayout.Label("(Select one in viewport)", st);
        GUILayout.Space(10);
        EditorGUILayout.PropertyField(roomP);

        so.ApplyModifiedProperties();
    }

    private void DuringSceneGUI(SceneView view)
    {
        Handles.BeginGUI();

        Rect rect = new Rect(10, 10, 150, 50);

        for (int i = 0; i < prefabs.Length; i++)
        {
            GameObject p = prefabs[i];

            Texture icon = AssetPreview.GetAssetPreview(p);

            EditorGUI.BeginChangeCheck();

            selectedPrefabs[i] = GUI.Toggle(rect, selectedPrefabs[i], new GUIContent(p.name, icon));

            //if(GUI.Button(rect, p.name))
            if (EditorGUI.EndChangeCheck())
            {
                room = null;
                for (int j = 0; j < selectedPrefabs.Length; j++)
                {
                    bool selectedOne = false;
                    if (selectedPrefabs[j])
                    {
                        if (selectedOne) selectedPrefabs[j] = false;
                        else room = prefabs[j].transform;
                        selectedOne = true;
                    }
                }
                //GenerateRPoints();
            }

            rect.y += rect.height + 2;
        }

        //GUI.Button(new Rect(10, 130, 100, 100), "A BUTTON");

        Handles.EndGUI();


        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

        Transform camTF = view.camera.transform;
        //Ray ray = new(camTF.position, camTF.forward);

        if (Event.current.type == EventType.MouseMove)
        {
            view.Repaint();
        }

        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector3 hitNormal = hit.normal;
            Vector3 hitTangent = Vector3.Cross(hitNormal, camTF.up).normalized;
            Vector3 hitBiTangent = Vector3.Cross(hitNormal, hitTangent);

            Ray GetTangentRay(Vector2 p)
            {
                Vector3 rayOrigin = (hit.point + p.x * hitTangent + p.y * hitBiTangent);
                rayOrigin += hitNormal * 2;
                Vector3 rayDir = -hit.normal;
                return new(rayOrigin, rayDir);
            }

            Vector3 positionedPoint = new();

            Ray ptRay = GetTangentRay(positionedPoint);

            if (Physics.Raycast(ptRay, out RaycastHit ptHit))
            {
                DrawPoint(ptHit.point);

                //Quaternion randomRot = GetRandomRot(ptHit.normal, p.angle);
                //SpawnPoint pose = new(ptHit.point, Quaternion.identity, positionedPoint);
                //positionedPoint.Add(pose);
                positionedPoint = ptHit.point;

                Handles.DrawAAPolyLine(6, ptHit.point, ptHit.point + ptHit.normal * 3);

                DrawPreview(positionedPoint);
            }

            if (Event.current.type == EventType.KeyUp & Event.current.keyCode == KeyCode.Space)
            {
                //SpawnPrefabs(positionedPoint);
                view.Repaint();
            }

            Handles.color = Color.red;
            Handles.DrawAAPolyLine(6, hit.point, hit.point + hitTangent);
            Handles.color = Color.green;
            Handles.DrawAAPolyLine(6, hit.point, hit.point + hitBiTangent);
            Handles.color = Color.blue;
            Handles.DrawAAPolyLine(6, hit.point, hit.point + hitNormal);

            Handles.color = Color.white;


            //Vector3[] points = new Vector3[circleDetail];
            //for (int i = 0; i < circleDetail; i++)
            //{
            //    float t = i / (float)(circleDetail) - 1;
            //    float angRad = t * DUEPI;
            //    Vector2 dir = new Vector2(Mathf.Cos(angRad), Mathf.Sin(angRad));
            //    Ray r = GetTangentRay(dir);

            //    if (Physics.Raycast(r, out RaycastHit cHit))
            //    {
            //        points[i] = cHit.point + cHit.normal * 0.02f;
            //    }
            //    else
            //    {
            //        points[i] = r.origin;
            //    }
            //}

            //Handles.DrawAAPolyLine(points);


            //Handles.DrawWireDisc(hit.point, hit.normal, radius);
            //Handles.DrawAAPolyLine(5, hit.point, hit.point + hit.normal * 2);
        }

        void DrawPoint(Vector3 pos)
        {
            Handles.SphereHandleCap(-1, pos, Quaternion.identity, .1f, EventType.Repaint);
        }
    }

    void DrawPreview(Vector3 pos)
    {
        //if (pose.spawnData.spawnPrefab == null || spawnPref == null || spawnPref.Count == 0) return;

        previewMaterial.SetPass(0);

        Matrix4x4 poseToWorldMtx = Matrix4x4.TRS(pos, Quaternion.identity, Vector3.one);

        MeshFilter[] filters = room.GetComponentsInChildren<MeshFilter>();

        foreach (MeshFilter f in filters)
        {
            Matrix4x4 childToPose = f.transform.localToWorldMatrix;
            Matrix4x4 childToWorldMatrix = poseToWorldMtx * childToPose;

            Mesh mesh = f.sharedMesh;
            Material m = f.GetComponent<MeshRenderer>().sharedMaterial;
            m.SetPass(0);
            Graphics.DrawMeshNow(mesh, childToWorldMatrix);
        }

        //Mesh mesh = prefab.GetComponent<MeshFilter>().sharedMesh;
        //Graphics.DrawMeshNow(mesh, ptHit.point, randomRot);
    }

    void SpawnPrefabs(Vector3 pose, GameObject pref)
    {
        //Undo.IncrementCurrentGroup();

        // CREAZIONE SENZA LINK AL PREFAB
        //Transform spawned = Instantiate(prefab, r.point, Quaternion.LookRotation(r.normal), null);
        //Undo.RegisterCreatedObjectUndo(spawned.gameObject, "Create my GameObject");

        // CREAZIONE CON LINK AL PREFAB
        Transform toSpawn = (Transform)PrefabUtility.InstantiatePrefab(pref);
        toSpawn.SetPositionAndRotation(pose, Quaternion.identity);
        Undo.RegisterCreatedObjectUndo(toSpawn.gameObject, "Spawn prefab");
    }
}