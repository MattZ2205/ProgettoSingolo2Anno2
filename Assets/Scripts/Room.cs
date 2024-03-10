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

[Serializable]
public struct AllWall
{
    public List<WallPiece> wall;
}

public class Room : MonoBehaviour
{
    [SerializeField] public List<AllWall> walls;
    [SerializeField] public float checkDistance;

    public void CheckOtherRooms()
    {
        for (int i = 0; i < walls.Count; i++)
        {
            for (int j = 0; j < walls[i].wall.Count; j++)
            {
                if (walls[i].wall[j].isDoor)
                {
                    if (Physics.Raycast(walls[i].wall[j].pieceOfWall.position, walls[i].wall[j].pieceOfWall.forward, checkDistance))
                    {
                        SnapRoom();
                        return;
                    }
                }
            }
        }
        //Debug.Log("Non ho trovato stanze");
        //DestroyImmediate(gameObject);
    }

    void SnapRoom()
    {
        Debug.Log("Snap!!!!!");
    }
}
