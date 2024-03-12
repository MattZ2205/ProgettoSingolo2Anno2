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

    public Transform room;
    public Material previewMaterial;

    SerializedObject so;
    Transform lastRoomPlaced;
    GameObject[] prefabs;
    bool[] selectedPrefabs;
    int selTog = -1;
    int movesDone = 0;

    private void OnEnable()
    {
        so = new SerializedObject(this);

        string[] guids = AssetDatabase.FindAssets("t:prefab", new[] { "Assets/Prefabs" });
        IEnumerable<string> paths = guids.Select(AssetDatabase.GUIDToAssetPath);
        prefabs = paths.Select(AssetDatabase.LoadAssetAtPath<GameObject>).ToArray();

        string[] guidsMat = AssetDatabase.FindAssets("t:material", new[] { "Assets/Materials" });
        IEnumerable<string> pathsMat = guidsMat.Select(AssetDatabase.GUIDToAssetPath);
        previewMaterial = pathsMat.Select(AssetDatabase.LoadAssetAtPath<Material>).ToArray()[0];

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
        if (room != null)
        {
            GUILayout.Space(10);
            GUILayout.Label("(Press space to place the object)", st);
            DrawBoxRoom();
        }
        //EditorGUILayout.PropertyField(roomP);
        //EditorGUILayout.PropertyField(matP);
        if (GUI.Button(new Rect(/*position.width / 2*/ 1, position.height - 30, position.width - 2, 30), "Undo") && movesDone > 0)
        {
            movesDone--;
            Undo.PerformUndo();
        }

        so.ApplyModifiedProperties();
    }

    void DrawBoxRoom()
    {
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = MakeTex(50, 50, Color.white);
        boxStyle.alignment = TextAnchor.MiddleCenter;
        boxStyle.normal.textColor = Color.grey;
        //GUI.color = Color.white;
        GUI.Box(new Rect((position.width / 2) - 40, (position.height / 2) - 40, 80, 80), room.transform.name, boxStyle);
        DrawDoors();
    }

    void DrawDoors()
    {
        Room actualRoom = room.transform.GetComponent<Room>();
        for (int i = 0; i < actualRoom.walls.Count; i++)
        {
            if (actualRoom.walls[i].isDoor)
            {
                GUIStyle doorStyle = new GUIStyle(GUI.skin.box);
                doorStyle.normal.background = MakeTex(25, 5, Color.red);
                Rect doorPos = CalculateDoorRect(i);
                GUI.Box(doorPos, GUIContent.none, doorStyle);
            }
        }
    }

    Rect CalculateDoorRect(int ind)
    {
        Rect pos = new Rect((position.width / 2) - 40, (position.height / 2) - 40, 0, 0);
        switch (ind)
        {
            case 0:
                pos.x += 20;
                pos.y -= 5;
                pos.width = 40;
                pos.height = 10;
                break;
            case 1:
                pos.x += 75;
                pos.y += 20;
                pos.width = 10;
                pos.height = 40;
                break;
            case 2:
                pos.x += 20;
                pos.y += 75;
                pos.width = 40;
                pos.height = 10;
                break;
            case 3:
                pos.x -= 5;
                pos.y += 20;
                pos.width = 10;
                pos.height = 40;
                break;
        }

        return pos;
    }

    Texture2D MakeTex(int width, int height, Color color)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; ++i)
        {
            pix[i] = color;
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
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

            if (EditorGUI.EndChangeCheck())
            {
                room = null;
                for (int j = 0; j < selectedPrefabs.Length; j++)
                {
                    if (selectedPrefabs[j])
                    {
                        if (i != selTog)
                        {
                            selectedPrefabs[j] = true;
                            if (selTog != -1) selectedPrefabs[selTog] = false;
                            selTog = j;
                            room = prefabs[j].transform;
                        }
                    }
                }
                Repaint();
            }

            rect.y += rect.height + 2;
        }

        Handles.EndGUI();

        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

        Transform camTF = view.camera.transform;

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
            if (room != null)
            {
                Ray ptRay = GetTangentRay(positionedPoint);

                if (Physics.Raycast(ptRay, out RaycastHit ptHit))
                {
                    DrawPoint(ptHit.point);

                    positionedPoint = new(ptHit.point.x, ptHit.point.y + (room.localScale.y / 2), ptHit.point.z);

                    Handles.DrawAAPolyLine(6, ptHit.point, ptHit.point + ptHit.normal * 3);

                    DrawPreview(positionedPoint);
                }

                if (Event.current.type == EventType.KeyUp & Event.current.keyCode == KeyCode.Space)
                {
                    SpawnPrefabs(positionedPoint, room);
                    view.Repaint();
                }
            }

            Handles.color = Color.red;
            Handles.DrawAAPolyLine(6, hit.point, hit.point + hitTangent);
            Handles.color = Color.green;
            Handles.DrawAAPolyLine(6, hit.point, hit.point + hitBiTangent);
            Handles.color = Color.blue;
            Handles.DrawAAPolyLine(6, hit.point, hit.point + hitNormal);

            Handles.color = Color.white;
        }

        void DrawPoint(Vector3 pos)
        {
            Handles.SphereHandleCap(-1, pos, Quaternion.identity, .1f, EventType.Repaint);
        }
    }

    void DrawPreview(Vector3 pos)
    {
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
    }

    void SpawnPrefabs(Vector3 pose, Transform pref)
    {
        Transform toSpawn = (Transform)PrefabUtility.InstantiatePrefab(pref);
        toSpawn.SetPositionAndRotation(pose, Quaternion.identity);
        Undo.RegisterCreatedObjectUndo(toSpawn.gameObject, "Spawn prefab");
        movesDone++;

        lastRoomPlaced = toSpawn;
        toSpawn.gameObject.GetComponent<Room>().CheckOtherRooms();
    }
}