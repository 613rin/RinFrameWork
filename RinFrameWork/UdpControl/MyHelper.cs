using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;

public static class MyHelper
{
    public static Texture2D[] LoadAllPicturesFromFile(string dirPath, bool deepSearch, params string[] searchPattern)
    {
        string[] allTexturesPath = SearchFilesWithPattern(dirPath, deepSearch, searchPattern);
        Texture2D[] textures = new Texture2D[allTexturesPath.Length];
        for (int i = 0; i < textures.Length; i++)
        {
            textures[i] = LoadPictureFromFile(allTexturesPath[i]);
            textures[i].name = allTexturesPath[i];
        }
        return textures;
    }
    public static string[] SearchFilesWithPattern(string dirPath, bool deepSearch, params string[] searchPattern)
    {
        SearchOption searchOption = deepSearch ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        List<string> allPath = new List<string>();
        foreach (var pattern in searchPattern)
        {
            allPath.AddRange(Directory.GetFiles(dirPath, pattern, searchOption));
        }
        return allPath.ToArray();
    }

    public static Vector2 EqualScaling(Vector2 max, Vector2 origin)
    {
        Vector2 size = new Vector2();
        size = EqualScalingWithWidth(max.x, origin);
        if (size.y > max.y)
        {
            size = EqualScalingWithHeight(max.y, origin);
        }
        return size;
    }
    public static Vector2 EqualScalingWithWidth(float max, Vector2 origin)
    {
        Vector2 size = new Vector2();
        size.x = max;
        size.y = size.x / origin.x * origin.y;
        return size;
    }
    public static Vector2 EqualScalingWithHeight(float max, Vector2 origin)
    {
        Vector2 size = new Vector2();
        size.y = max;
        size.x = size.y / origin.y * origin.x;
        return size;
    }
    public static string SearchFileWithFileName(string dirPath, string fileName)
    {
        if (Directory.Exists(dirPath))
        {
            foreach (var file in Directory.GetFiles(dirPath))
            {
                if (Path.GetFileName(file) == fileName)
                    return file;
            }
        }
        return null;
    }
    public static string SearchFileWithPattern(string dirPath, bool deepSearch, string searchPattern)
    {
        SearchOption searchOption = deepSearch ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        string[] files = Directory.GetFiles(dirPath, searchPattern, searchOption);
        foreach (var file in files)
        {
            return file;
        }
        return null;
    }
    public static Texture2D LoadPictureFromFile(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        Texture2D texture = new Texture2D(0, 0);
        texture.LoadImage(bytes);
        return texture;
    }
    public static Texture2D[] LoadPictureFromFile(string[] paths)
    {
        Texture2D[] textures = new Texture2D[paths.Length];
        for (int i = 0; i < paths.Length; i++)
        {
            string path = paths[i];
            byte[] bytes = File.ReadAllBytes(path);
            Texture2D texture = new Texture2D(0, 0);
            texture.LoadImage(bytes);
            textures[i] = texture;
        }
        return textures;
    }

    public static T LoadConfig<T>(string path, string name)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("没有找到配置文件");
        }
        string[] lines = File.ReadAllLines(path);
        foreach (var line in lines)
        {
            if (line.Contains("="))
            {
                string[] sps = line.Split('=');
                if (sps[0].ToUpper() == name.ToUpper())
                    if (typeof(T) == typeof(int))
                    {
                        int value = 0;
                        if (int.TryParse(sps[1], out value))
                            return (T)((object)value);
                    }
                    else if (typeof(T) == typeof(float))
                    {
                        float value = 0;
                        if (float.TryParse(sps[1], out value))
                            return (T)((object)value);
                    }
                    else if (typeof(T) == typeof(bool))
                    {
                        bool value = false;
                        if (bool.TryParse(sps[1], out value))
                            return (T)((object)value);
                    }
                    else if (typeof(T) == typeof(string))
                        return (T)(object)sps[1];
            }
        }
        throw new System.Exception("没有找到对应值");
    }


    public static List<T> FindAllTypes<T>()
    {
        List<T> interfaces = new List<T>();

        var types = UnityEngine.MonoBehaviour.FindObjectsOfType<MonoBehaviour>().OfType<T>();
        foreach (T t in types)
        {
            interfaces.Add(t);
        }

        return interfaces;
    }
    public static Texture2D ScaleTextureBilinearWithWidth(Texture2D originalTexture, int width)
    {
        if (originalTexture.width > width)
        {
            float scaleFactor = (float)width / (float)originalTexture.width;
            return ScaleTextureBilinear(originalTexture, scaleFactor);
        }
        else
        {
            return originalTexture;
        }
    }

    public static Texture2D ScaleTextureBilinear(Texture2D originalTexture, float scaleFactor)
    {
        Texture2D newTexture = new Texture2D(Mathf.CeilToInt(originalTexture.width * scaleFactor), Mathf.CeilToInt(originalTexture.height * scaleFactor));
        float scale = 1.0f / scaleFactor;
        int maxX = originalTexture.width - 1;
        int maxY = originalTexture.height - 1;
        List<Color> colors = new List<Color>();
        for (int y = 0; y < newTexture.height; y++)
        {
            for (int x = 0; x < newTexture.width; x++)
            {
                // Bilinear Interpolation
                float targetX = x * scale;
                float targetY = y * scale;
                int x1 = Mathf.Min(maxX, Mathf.FloorToInt(targetX));
                int y1 = Mathf.Min(maxY, Mathf.FloorToInt(targetY));
                int x2 = Mathf.Min(maxX, x1 + 1);
                int y2 = Mathf.Min(maxY, y1 + 1);

                float u = targetX - x1;
                float v = targetY - y1;
                float w1 = (1 - u) * (1 - v);
                float w2 = u * (1 - v);
                float w3 = (1 - u) * v;
                float w4 = u * v;
                Color color1 = originalTexture.GetPixel(x1, y1);
                Color color2 = originalTexture.GetPixel(x2, y1);
                Color color3 = originalTexture.GetPixel(x1, y2);
                Color color4 = originalTexture.GetPixel(x2, y2);
                Color color = new Color(Mathf.Clamp01(color1.r * w1 + color2.r * w2 + color3.r * w3 + color4.r * w4),
                    Mathf.Clamp01(color1.g * w1 + color2.g * w2 + color3.g * w3 + color4.g * w4),
                    Mathf.Clamp01(color1.b * w1 + color2.b * w2 + color3.b * w3 + color4.b * w4),
                    Mathf.Clamp01(color1.a * w1 + color2.a * w2 + color3.a * w3 + color4.a * w4)
                    );
                //newTexture.SetPixel(x, y, color);
                colors.Add(color);
            }
        }
        newTexture.SetPixels(colors.ToArray());
        newTexture.Apply();
        return newTexture;
    }
}