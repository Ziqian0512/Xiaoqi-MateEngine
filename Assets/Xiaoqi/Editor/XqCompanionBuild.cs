using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Xiaoqi.UI2;

public static class XqCompanionBuild
{
    public static void VerifyAndBuild()
    {
        foreach(string path in Directory.GetFiles("Assets/Xiaoqi/Resources/CloseCompanion","*.png"))
        {
            var importer=AssetImporter.GetAtPath(path) as TextureImporter;
            if(importer==null)throw new InvalidOperationException("Companion texture missing: "+path);
            importer.npotScale=TextureImporterNPOTScale.None;importer.mipmapEnabled=false;
            importer.textureCompression=TextureImporterCompression.Uncompressed;importer.maxTextureSize=4096;
            importer.alphaIsTransparency=true;importer.alphaSource=TextureImporterAlphaSource.FromInput;
            importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
            var platform=importer.GetPlatformTextureSettings("Standalone");platform.overridden=false;importer.SetPlatformTextureSettings(platform);importer.SaveAndReimport();
            var original=new Texture2D(2,2);ImageConversion.LoadImage(original,File.ReadAllBytes(path));
            var actual=AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if(original.width!=actual.width||original.height!=actual.height)throw new InvalidOperationException("Companion texture scaled: "+path);
            Debug.Log("XQ_COMPANION_TEXTURE preserved "+Path.GetFileName(path)+" "+actual.width+"x"+actual.height);
            UnityEngine.Object.DestroyImmediate(original);
        }
        if(XqVoiceChatClient.ParseUsage("{\"usage\":{\"prompt_tokens\":12,\"completion_tokens\":8,\"total_tokens\":20}}")?.totalTokens!=20)throw new Exception("Usage parse failed");
        foreach(var json in new[]{"{}","broken","{\"usage\":{\"total_tokens\":99}}","{\"usage\":{\"prompt_tokens\":-1,\"completion_tokens\":8,\"total_tokens\":7}}","{\"usage\":{\"prompt_tokens\":0,\"completion_tokens\":0,\"total_tokens\":0}}","{\"usage\":{\"prompt_tokens\":12,\"completion_tokens\":8,\"total_tokens\":80}}"})
            if(XqVoiceChatClient.ParseUsage(json)!=null)throw new Exception("Invalid usage must remain unknown");
        Debug.Log("XQ_COMPANION_USAGE 7 checks passed");
        XqVoicePolicyChecks.VerifyAndBuild();
    }
}
