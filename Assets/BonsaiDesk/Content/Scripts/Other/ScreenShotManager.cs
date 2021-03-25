using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class ScreenShotManager : MonoBehaviour
{
    public bool active;
    public ScreenShotUtility screen;
    public string folder;
    public RectTransform UIHolder;
    public int xResolution;
    public int yResolution;
    public string singleScreenShotName;

    Phone[] phones =
    {
        new Phone("7 inch android tablet", 2048, 1536),
        new Phone("10 inch android tablet", 2224, 1668),
        new Phone("android phone", 1920, 1080),
        new Phone("6.5 inch iPhone", 2688, 1242),
        new Phone("5.5 inch iPhone", 2208, 1242),
        new Phone("12.9 inch iPad 3rd Gen", 2732, 2048),
        new Phone("12.9 inch iPad 2rd Gen", 2732, 2048),
        new Phone("1440p", 2560, 1440)
    };
    
    void Start()
    {
        screen.folder = folder;

        if (!Application.isEditor || !active)
            return;
    }

    void Update()
    {
        if (!Application.isEditor || !active)
            return;

        if (Input.GetKeyDown(KeyCode.K))
            takeAllScreenShots();
        //if (Input.GetKeyDown(KeyCode.L))
        //    takeScreenshot(xResolution, yResolution, singleScreenShotName, false);
    }
    
    void takeAllScreenShots()
    {
        for (int i = 0; i < phones.Length; i++)
        {
            string path = Path.Combine(folder, phones[i].name);
            Directory.CreateDirectory(path);
            screen.folder = path;
            int unixTime = (int)(System.DateTime.UtcNow - new System.DateTime(1970, 1, 1, 0, 0, 0, System.DateTimeKind.Utc)).TotalSeconds;
            takeScreenshot(phones[i].screenWidth, phones[i].screenHeight, phones[i].name + " " + unixTime, true);
        }
        
        UIHolder.anchorMin = new Vector2(0, 0);
        UIHolder.anchorMax = new Vector2(1, 1);
    }

    void takeScreenshot(int captureWidth, int captureHeight, string screenShotName, bool changeAspectRatio)
    {
        if (changeAspectRatio)
        {
            float aspectRatio = 1 - (float)captureWidth / (float)captureHeight;
            UIHolder.anchorMin = new Vector2(aspectRatio / 2f, 0);
            UIHolder.anchorMax = new Vector2(1 - aspectRatio / 2f, 1);
        }

        screen.takeScreenshot(captureWidth, captureHeight, screenShotName);

    }
}

class Phone
{
    public string name;
    public int screenWidth;
    public int screenHeight;

    public Phone(string name, int screenWidth, int screenHeight)
    {
        this.name = name;
        this.screenWidth = screenWidth;
        this.screenHeight = screenHeight;
    }
}
