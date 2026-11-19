using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class XqSweetNotebookBuild
{
    public static void VerifyAndBuild()
    {
        foreach(string path in Directory.GetFiles("Assets/Xiaoqi/Resources/SweetNotebook","*.png"))
        {
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null)throw new InvalidOperationException("Sweet texture missing: "+path);
            importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=false;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;
            importer.alphaIsTransparency=true;importer.alphaSource=TextureImporterAlphaSource.FromInput;
            importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
            var platform=importer.GetPlatformTextureSettings("Standalone");platform.overridden=false;importer.SetPlatformTextureSettings(platform);
            importer.SaveAndReimport();
            var original=new Texture2D(2,2);ImageConversion.LoadImage(original,File.ReadAllBytes(path));
            var actual=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(original.width!=actual.width||original.height!=actual.height)throw new InvalidOperationException("Sweet texture scaled: "+path);
            Debug.Log("XQ_SWEET_TEXTURE preserved "+Path.GetFileName(path)+" "+actual.width+"x"+actual.height);
            UnityEngine.Object.DestroyImmediate(original);
        }
        XqVoicePolicyChecks.VerifyAndBuild();
    }
}
