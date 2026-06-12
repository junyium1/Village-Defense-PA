using System.Collections;
using System.IO;
using UnityEngine;

public class ShareButton : MonoBehaviour
{
    private IEnumerator screenNshare()
    {
        yield return new WaitForEndOfFrame();
        
        Texture2D ss = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);
        ss.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        ss.Apply();
        
        string filePath = Path.Combine(Application.streamingAssetsPath, "shareimage.png");
        File.WriteAllBytes(filePath, ss.EncodeToPNG());
        
        Destroy(ss);
        
        new NativeShare().AddFile(filePath).SetSubject("VillageDefender").SetText("Kill yourself").Share();
    }
    
}
