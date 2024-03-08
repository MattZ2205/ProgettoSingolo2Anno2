using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

[Serializable]
public struct WallPiece
{
    public bool isDoor;
    public Transform pieceOfWall;
}

//[Serializable]
//public struct AllWall
//{
//    public List<WallPiece> wall;
//}

public class Room : MonoBehaviour
{
    public List<WallPiece> walls;

    [SerializeField] float checkDistance;
    [SerializeField] float distancefromRooms;
    [SerializeField] LayerMask roomMask;

    public void CheckOtherRooms()
    {
        for (int i = 0; i < walls.Count; i++)
        {
            //for (int j = 0; j < walls[i].wall.Count; j++)
            //{
            if (walls[i]/*.wall[j]*/.isDoor)
            {
                if (CheckDoors(i)) return;
            }
            //}
        }
        Debug.Log("Non ho trovato stanze");
        DestroyImmediate(gameObject);
    }

    bool CheckDoors(int ind)
    {
        Debug.DrawRay(walls[ind].pieceOfWall.position, walls[ind].pieceOfWall.forward * checkDistance, Color.red, 2);
        if (Physics.Raycast(walls[ind].pieceOfWall.position, walls[ind].pieceOfWall.forward, out RaycastHit hit, checkDistance, roomMask))
        {
            Room hitted = hit.transform.gameObject.GetComponent<Room>();
            if (hitted != null)
            {
                Debug.Log(transform.name + " hitted " + hitted.transform.name);
                int indCheck = ind < 2 ? ind + 2 : ind - 2;
                if (hitted.walls[indCheck].isDoor)
                {
                    SnapRoom(hitted.transform, ind);
                    return true;
                }
            }
        }
        return false;
    }

    void SnapRoom(Transform hitted, int i)
    {
        Vector3 finalPos = hitted.position;
        switch (i)
        {
            case 0:
                finalPos.z -= distancefromRooms;
                break;
            case 1:
                finalPos.x -= distancefromRooms;
                break;
            case 2:
                finalPos.z += distancefromRooms;
                break;
            case 3:
                finalPos.x += distancefromRooms;
                break;
        }

        transform.position = finalPos;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position, new(distancefromRooms, distancefromRooms, distancefromRooms));
    }
}
