using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FrameWorkSong;

public class LineTest : MonoBehaviour
{
    LineCreateOf3D LineCreateOf3D = new LineCreateOf3D();
    public Transform[] transforms;
    public Material material;
    GameObject game;
    public int Segments = 6;
    public float Radius = 1;
    public float elbow_Radius = 4;
    // Start is called before the first frame update
    void Start()
    {

    }
    float t = 0;
    // Update is called once per frame
    void Update()
    {
        game = test();
        if (t > 10)
        {
            t = 0;
        }
        material.mainTextureOffset = new Vector2(0, -t);
        t += 3 * Time.deltaTime;

    }
    GameObject test()
    {
        if (game)
        {
            Destroy(game);
        }
        int length = transforms.Length;
        Vector3[] point = new Vector3[length];
        for (int i = 0; i < length; i++)
        {
            point[i] = transforms[i].position;
        }
        game = LineCreateOf3D.CreateLine(point, material, Segments, Radius, elbow_Radius);
        game.name = "11";
        return game;
    }
}
