using UnityEngine;
using System.IO;
using System;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Collections;


namespace FFMpegUtils
{
    public class FFMpegYUV4Texture : MonoBehaviour
    {
        [SerializeField]
        Renderer quad;

        [SerializeField]
        string Source;

        const string KEY = "FFMPEG_DIR";
        const string EXE = "ffmpeg.exe";

        #region StdErrStream
        List<Byte> m_error = new List<byte>();
        static void OnRead(List<Byte> queue, Byte[] buffer, int count)
        {
            lock (((ICollection)queue).SyncRoot)
            {
                queue.AddRange(buffer.Take(count));
            }
        }
        static Byte[] Dequeue(List<Byte> queue)
        {
            Byte[] tmp;
            lock (((ICollection)queue).SyncRoot)
            {
                tmp = queue.ToArray();
                queue.Clear();
            }
            return tmp;
        }
        #endregion

        [SerializeField]
        Texture2D Texture;

        FFMpegLauncher m_ffmpeg;

        [SerializeField]
        YUVReader m_yuvReader;

        IDisposable m_stdOutDisposable;
        IDisposable m_stdErrDisposable;
        int m_lastYUVFrame = -1;

        void OnEnable()
        {
            m_yuvReader = new YUVReader();

            // launch
            var exec = Path.Combine(Environment.GetEnvironmentVariable(KEY), EXE);
            m_ffmpeg = FFMpegLauncher.Launch(exec, "-i \"{0}\" -f yuv4mpegpipe -", Source);
            if (m_ffmpeg == null)
            {
                Debug.LogWarning("fail to launch ffmpeg");
                return;
            }

            m_stdOutDisposable = m_ffmpeg.StdOut.BeginRead(new Byte[8192], (b, c) => m_yuvReader.Push(new ArraySegment<byte>(b, 0, c)));
            m_stdErrDisposable = m_ffmpeg.StdErr.BeginRead(new Byte[1024], (b, c) => OnRead(m_error, b, c));
        }

        void Update()
        {
            var error = Dequeue(m_error);
            if (error.Any())
            {
                var text = Encoding.UTF8.GetString(error, 0, error.Length);
                Debug.LogWarning(text);
            }

            if (Texture == null)
            {
                // create texture
                if (m_yuvReader.Header != null)
                {
                    Texture = new Texture2D(m_yuvReader.Header.Width, m_yuvReader.Header.Height * 3 / 2, TextureFormat.Alpha8, false);
                    quad.material.mainTexture = Texture;
                }
            }
            else { 
                var frame = m_yuvReader.GetFrame();
                if (frame.FrameNumber != m_lastYUVFrame)
                {
                    Texture.LoadRawTextureData(frame.Bytes);
                    Texture.Apply();
                }
            }
        }

        void OnDisable()
        {
            Debug.Log("kill");
            if (m_ffmpeg != null)
            {
                m_ffmpeg.Dispose();
                m_ffmpeg = null;
            }
            if(m_stdOutDisposable!= null)
            {
                m_stdOutDisposable.Dispose();
                m_stdOutDisposable = null;
            }
            if(m_stdErrDisposable!= null)
            {
                m_stdErrDisposable.Dispose();
                m_stdErrDisposable = null;
            }
        }
    }
}
