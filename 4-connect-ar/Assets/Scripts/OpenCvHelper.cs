using OpenCvSharp;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OpenCvHelper : MonoBehaviour
{
    private static Texture2D _Overlay;

    public static Texture2D Overlay
    {
        get 
        { 
            return _Overlay; 
        }
        set 
        {
            if (_Overlay != null)
            {
                Destroy(_Overlay);
            }
            _Overlay = value; 
        }
    }

    public static Mat ConvertImageToMat(RawImage image)
    {
        Texture2D text = ToTexture2D(image.texture);
        Mat matrix = OpenCvSharp.Unity.TextureToMat(text);

        Destroy(text);
        return matrix;
    }

    public static Texture2D ToTexture2D(Texture texture)
    {
        Texture2D texture2d = new Texture2D(texture.width, texture.height, TextureFormat.ARGB32, false);

        Graphics.CopyTexture(texture, texture2d);

        return texture2d;
    }
}
