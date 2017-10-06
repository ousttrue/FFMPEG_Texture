using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using FFMpegUtils;

// This is an example of how to setup FFMPEG_Texture at runtime. This can be useful if you want to
// allow the user to enter the URL of a video stream to connect to, start a video on command, or
// anything else that requires that the video not be started immediately.
public class RuntimeFFMPEGTexture : MonoBehaviour
{
    FFMpegYUV4Texture ffmpegTexture;
    bool initialized = false;

    void Start ()
    {
        // Instantiate the material and assign it to the host object
        ffmpegTexture = gameObject.AddComponent<FFMpegYUV4Texture>();
        this.GetComponent<Renderer>().material = ffmpegTexture.GetMaterial();
    }
	
	void Update ()
    {
        // wait three seconds and then Initialize
	    if (Time.time > 3 && !initialized)
        {
            ffmpegTexture.Initialize("C:\\temp\\sample2.mp4");
            initialized = true;
        }
    }
}
