using System;
using UnityEngine;

[Serializable]
public class CityMapLayout
{
    public string cityScene = "MainScene(city)";
    public Vector2 min, max, home;
    public Shape[] shapes;
    public Place[] places;

    [Serializable]
    public class Shape
    {
        public int kind;
        public Vector2[] points;
    }

    [Serializable]
    public class Place
    {
        public string name;
        public Vector2 position;
    }
}
