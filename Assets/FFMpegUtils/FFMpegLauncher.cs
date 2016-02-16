using System.Collections;
using System;
using System.IO;
using System.Text;
using System.Diagnostics;

namespace FFMpegUtils
{
    public static class RecursiveReader
    {
        public delegate void OnReadFunc(Byte[] bytes, int count);

        public delegate void OnCompleteFunc();

        public static void BeginRead(this Stream s, Byte[] buffer, OnReadFunc onRead, OnCompleteFunc onComplete=null)
        {
            AsyncCallback callback = ar => {
                var ss = ar.AsyncState as Stream;
                var readCount = ss.EndRead(ar);
                if (readCount == 0)
                {
                    if (onComplete != null)
                    {
                        onComplete();
                    }
                    return;
                }

                onRead(buffer, readCount);

                BeginRead(ss, buffer, onRead, onComplete);
            };
            s.BeginRead(buffer, 0, buffer.Length, callback, s);
        }
    }

    public class FFMpegLauncher : IDisposable
    {
        Process m_process;

        public Stream StdOut
        {
            get
            {
                return m_process.StandardOutput.BaseStream;
            }
        }

        public Stream StdErr
        {
            get
            {
                return m_process.StandardError.BaseStream;
            }
        }

        FFMpegLauncher(ProcessStartInfo startInfo)
        {
            m_process=System.Diagnostics.Process.Start(startInfo);
        }

        public static FFMpegLauncher Launch(String exec, String argFmt, params String[] args)
        {
            var file = new FileInfo(exec);
            if (!file.Exists)
            {
                return null;
            }

            var startInfo = new ProcessStartInfo(file.FullName, String.Format("-i \"{0}\" -f yuv4mpegpipe -", args))
            {
                CreateNoWindow = true,
                RedirectStandardError = true,
                StandardErrorEncoding = Encoding.UTF8,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };

            return new FFMpegLauncher(startInfo);
        }

        #region IDisposable Support
        private bool disposedValue = false; // 重複する呼び出しを検出するには

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: マネージ状態を破棄します (マネージ オブジェクト)。
                    if (m_process != null)
                    {
                        m_process.Dispose();
                        m_process = null;
                    }
                }

                // TODO: アンマネージ リソース (アンマネージ オブジェクト) を解放し、下のファイナライザーをオーバーライドします。
                // TODO: 大きなフィールドを null に設定します。

                disposedValue = true;
            }
        }

        // TODO: 上の Dispose(bool disposing) にアンマネージ リソースを解放するコードが含まれる場合にのみ、ファイナライザーをオーバーライドします。
        // ~FFMpegLauncher() {
        //   // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
        //   Dispose(false);
        // }

        // このコードは、破棄可能なパターンを正しく実装できるように追加されました。
        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを上の Dispose(bool disposing) に記述します。
            Dispose(true);
            // TODO: 上のファイナライザーがオーバーライドされる場合は、次の行のコメントを解除してください。
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
