using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class XqCampusPreviewBuild
{
    public static void VerifyAndBuild()
    {
        VerifyTexture("Assets/Xiaoqi/Resources/XqCampusRoom.png",false);
        VerifyTexture("Assets/Xiaoqi/Resources/XqCampusInori.png",true);
        XqVoicePolicyChecks.VerifyAndBuild();
    }
    static void VerifyTexture(string path,bool alpha)
    {
        var importer=AssetImporter.GetAtPath(path) as TextureImporter;
        if(importer==null)throw new InvalidOperationException("Campus texture importer missing: "+path);
        importer.npotScale=TextureImporterNPOTScale.None;
        importer.mipmapEnabled=false;importer.textureCompression=TextureImporterCompression.Uncompressed;
        importer.maxTextureSize=2048;importer.alphaIsTransparency=alpha;
        importer.alphaSource=TextureImporterAlphaSource.FromInput;
        importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
        var platform=importer.GetPlatformTextureSettings("Standalone");platform.overridden=false;importer.SetPlatformTextureSettings(platform);
        importer.SaveAndReimport();
        var source=new Texture2D(2,2);ImageConversion.LoadImage(source,File.ReadAllBytes(path));
        var imported=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if(imported.width!=source.width||imported.height!=source.height)throw new InvalidOperationException("Campus texture dimensions changed: "+path);
        Debug.Log("XQ_CAMPUS_TEXTURE dimensions preserved "+Path.GetFileName(path)+" "+imported.width+"x"+imported.height);
        UnityEngine.Object.DestroyImmediate(source);
    }
}
