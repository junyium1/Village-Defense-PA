using System.Collections;
using System.IO;
using UnityEngine;

public class ShareButton : MonoBehaviour
{
    private string _sharemessage;

    public void ClickShareButton()
    {
        _sharemessage = "12000 bees waiting in front of your house have a message for you : Come outside."; // ajouter "+ varscore" pour output le score obtenu. 
        StartCoroutine(ScreenNshare());
    }
    
    
    private IEnumerator ScreenNshare()
    {
        yield return new WaitForEndOfFrame();
        
        Texture2D ss = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        ss.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        ss.Apply();
        
        string filePath = Path.Combine(Application.persistentDataPath, "shareimage.png").Replace("/", "\\");
        File.WriteAllBytes(filePath, ss.EncodeToPNG());
        
        Destroy(ss);
        
        #if UNITY_STANDALONE_WIN
        var psi = new System.Diagnostics.ProcessStartInfo()
        {
            FileName = "explorer.exe",
            Arguments = $"/select,\"{filePath}\""
        };
        System.Diagnostics.Process.Start(psi);
        
        string encoded = System.Uri.EscapeDataString(_sharemessage); 
        Application.OpenURL($"https://twitter.com/intent/tweet?text={encoded}");
        #endif
    }
}
