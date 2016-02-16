    using UnityEngine;
    using System.IO;
    using System;
    using System.Linq;
    using System.Text;
    using System.Collections.Generic;
    using System.Collections;


namespace FFMpegUtils {
    public class FFMpegYUV4Texture : MonoBehaviour
    {
        [SerializeField]
        Renderer quad;

       [SerializeField]
        string Source;

        const string KEY = "FFMPEG_DIR";
        const string EXE = "ffmpeg.exe";

        List<Byte> m_queue = new List<byte>();
        List<Byte> m_error = new List<byte>();

        [SerializeField]
        Texture2D Texture;

        FFMpegLauncher m_ffmpeg;

        // ffmpeg -v 0 -i "{0}" -pix_fmt yuv420p -an -vcodec rawvideo -f yuv4mpegpipe -
        void Awake()
        {
            // clear
            m_header = null;
        }

        void OnEnable()
        { 
            // launch
            var exec = Path.Combine(Environment.GetEnvironmentVariable(KEY), EXE);
            m_ffmpeg = FFMpegLauncher.Launch(exec, "-i \"{0}\" -f yuv4mpegpipe -", Source);
            if(m_ffmpeg== null)
            {
                Debug.LogWarning("fail to launch ffmpeg");
                return;
            }

            m_ffmpeg.StdOut.BeginRead(new Byte[8192], (b, c) => OnRead(m_queue, b, c));
            m_ffmpeg.StdErr.BeginRead(new Byte[1024], (b, c) => OnRead(m_error, b, c));
        }

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

        public enum YUVFormat
        {
            YUV420,
        }

        [Serializable]
        public class Frame
        {
            [SerializeField]
            List<Byte> m_header = new List<byte>();

            int m_fill;
            Byte[] m_body;
            public Byte[] Body
            {
                get { return m_body; }
            }

            public bool IsFill
            {
                get
                {
                    return m_fill >= m_body.Length;
                }
            }

            bool m_isHeader;

            public Frame(int bytes)
            {
                m_body = new Byte[bytes];
            }

            public void Clear()
            {
                m_isHeader = true;
                m_header.Clear();
                m_fill = 0;
            }

            public int Push(Byte[] bytes, int i)
            {
                if (m_isHeader) {
                    for (; i < bytes.Length; ++i)
                    {
                        if (bytes[i] == 0x0a) {
                            m_isHeader = false;
                            ++i;
                            break;
                        }
                        m_header.Add(bytes[i]);
                    }
                }

                for (; i < bytes.Length && m_fill < m_body.Length; ++i, ++m_fill)
                {
                    m_body[m_fill] = bytes[i];
                }

                return i;
            }
        }

        [Serializable]
        public class YUVInfo
        {
            public YUVFormat Format;
            public int Width;
            public int Height;

            public YUVInfo(int w, int h, YUVFormat format)
            {
                Width = w;
                Height = h;
                Format = format;
                m_current = new Frame(FrameBodyLength);
                m_next = new Frame(FrameBodyLength);
            }

            List<Byte> m_buffer = new List<byte>();

            public int FrameBodyLength
            {
                get
                {
                    return Width * Height * 3 / 2;
                }
            }

            public override string ToString()
            {
                return String.Format("[{0}]{1}x{2}", Format, Width, Height);
            }

            public Texture2D CreateTexture()
            {
                return new Texture2D(Width, Height * 3 / 2, TextureFormat.Alpha8, false);
            }

            readonly Byte[] frame_header = new[] { (byte)0x46, (byte)0x52, (byte)0x41, (byte)0x4D, (byte)0x45 };

            [SerializeField]
            Frame m_current;

            [SerializeField]
            Frame m_next;

            bool IsHead(List<Byte> src)
            {
                for (int i = 0; i < frame_header.Length; ++i)
                {
                    if (src[i] != frame_header[i]) return false;
                }
                return true;
            }

            public bool Push(Byte[] bytes)
            {
                bool hasNewFrame = false;

                var i = 0;
                while (i < bytes.Length)
                {
                    i = m_next.Push(bytes, i);
                    if (m_next.IsFill)
                    {
                        var tmp = m_current;
                        m_current = m_next;
                        m_next = tmp;
                        m_next.Clear();

                        hasNewFrame = true;
                    }
                }

                return hasNewFrame;
            }

            public void Apply(Texture2D texture)
            {
                if (texture != null)
                {
                    texture.LoadRawTextureData(m_current.Body);
                    texture.Apply();
                }
            }

            public static YUVInfo Parse(String header)
            {
                Debug.Log("[Parse]" + header);

                int width = 0;
                int height = 0;
                var format = default(YUVFormat);
                foreach (var value in header.Split())
                {
                    switch (value.FirstOrDefault())
                    {
                        case 'W':
                            try {
                                width = int.Parse(value.Substring(1));
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError(ex + ": " + value);
                                throw;
                            }
                            break;

                        case 'H':
                            try {
                                height = int.Parse(value.Substring(1));
                            }
                            catch (Exception ex)
                            {
                                Debug.LogError(ex + ": " + value);
                                throw;
                            }
                            break;

                        case 'C':
                            if (value == "C420mpeg2")
                            {
                                format = YUVFormat.YUV420;
                            }
                            break;
                    }
                }
                return new YUVInfo(width, height, format);
            }
        }

        [SerializeField]
        YUVInfo m_header;

        // Update is called once per frame
        void Update() {

            {
                var data = Dequeue(m_queue);
                if (data != null && data.Any())
                {
                    if (m_header == null)
                    {
                        var tmp = data.TakeWhile(x => x != 0x0A).ToArray();
                        m_header = YUVInfo.Parse(Encoding.ASCII.GetString(tmp));
                        Debug.Log(m_header);

                        data = data.Skip(tmp.Length).ToArray();

                        // create texture
                        Texture = m_header.CreateTexture();
                        quad.material.mainTexture = Texture;
                    }

                    // parse data
                    if (m_header.Push(data))
                    {
                        m_header.Apply(Texture);
                    }
                }
            }

            {
                var error = Dequeue(m_error);
                if (error.Any())
                {
                    var text = Encoding.UTF8.GetString(error, 0, error.Length);
                    Debug.LogWarning(text);
                }
            }
        }

        void OnDisable()
        {
            Debug.Log("kill");
            if(m_ffmpeg!= null)
            {
                m_ffmpeg.Dispose();
                m_ffmpeg = null;
            }
        }
    }
}
