using System.Text;

namespace textFileReadWrite
{
    // 참조추가
    //using System.Runtime.InteropServices;
    //using System.Windows.Forms;

    public class CONFIG
    {
        Mutex mutex;

        protected string home_path;
        protected string dir_path;
        protected string file_name;
        protected string path;

        public DateTime LastReadTime { get; set; } = new DateTime();
        public DateTime LastWriteTime
        {
            get
            {
                try
                {
                    return System.IO.File.GetLastWriteTimeUtc(path);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[WARNING] {0}: {1}", GetType().Name, ex.Message);
                    return new DateTime();
                }
            }
        }

        /// <summary>
        /// // new CONFIG(string "설정 파일명", string "폴더명:기본값etc")
        /// </summary>
        public CONFIG(string filePath, string _file_name, string _dir_path = "etc")
        {
            dir_path = _dir_path;
            file_name = _file_name;

            path = System.IO.Path.Combine(filePath, dir_path, file_name);

            // 디렉토리 생성
            if (System.IO.Directory.Exists(dir_path) == false)
            {
                try
                {
                    System.IO.Directory.CreateDirectory(dir_path);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("[WARNING] {0}: {1}", GetType().Name, ex.Message);

                    path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), file_name);
                    Console.WriteLine("[INFO] CONFIG(path) := {0}", path);
                }
            }

            // 뮤텍스 생성
            try
            {
                mutex = new System.Threading.Mutex(false, path.Replace("\\", "_"));
            }
            catch (Exception ex)
            {
                Console.WriteLine("[WARNING] {0}: {1}", GetType().Name, ex.Message);
            }
        }

        ~CONFIG()
        {
            if (mutex != null)
            {
                mutex.Dispose();
            }
        }

        public bool LockMutex()
        {
            try
            {
                if (mutex.WaitOne(100, true) == true)
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("[WARNING] {0}: {1}", GetType().Name, ex.Message);
            }

            return false;
        }

        public void ReleaseMutex()
        {
            try
            {
                mutex.ReleaseMutex();
            }
            catch (Exception ex)
            {
                Console.WriteLine("[WARNING] {0}: {1}", GetType().Name, ex.Message);
            }
        }

        [System.Runtime.InteropServices.DllImport("Kernel32.dll")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        [System.Runtime.InteropServices.DllImport("Kernel32.dll")]
        private static extern int WritePrivateProfileString(string section, string key, string value, string filepath);

        [System.Runtime.InteropServices.DllImport("Kernel32.dll")]
        private static extern int GetLastError();

        /// <summary>
        /// 문자열을 읽습니다.
        /// </summary>
        /// <param name="section">섹션</param>
        /// <param name="key">키</param>
        /// <param name="defaultValue">기본값</param>
        /// <returns></returns>
        public string ReadString(string section, string key, string defaultValue)
        {
            StringBuilder str = new StringBuilder(100, 8192);

            try
            {
                if (GetPrivateProfileString(section, key, string.Empty, str, str.MaxCapacity, path) == 0)
                {
                    WritePrivateProfileString(section, key, defaultValue, path);

                    return defaultValue;
                }
            }
            catch
            {
                Console.WriteLine(GetLastError());
            }

            return str.ToString();
        }

        /// <summary>
        /// 문자열을 기록합니다.
        /// </summary>
        /// <param name="section">섹션</param>
        /// <param name="key">키</param>
        /// <param name="value">키에 해당하는 값</param>
        /// <returns></returns>
        public bool WriteString(string section, string key, string value)
        {
            try
            {
                WritePrivateProfileString(section, key, value, path);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[WARNING] {0}: {1}: {2}", GetType().Name, file_name, ex.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 암호화된 기록을 읽습니다.
        /// </summary>
        /// <param name="section">섹션</param>
        /// <param name="key">키</param>
        /// <param name="defaultValue">키가 없을 경우 기본값</param>
        /// <returns></returns>
        public string ReadPassword(string section, string key, string defaultValue)
        {
            StringBuilder str = new StringBuilder(100, 8192);
            StringBuilder sb = new StringBuilder();

            try
            {
                if (GetPrivateProfileString(section, key, string.Empty, str, str.MaxCapacity, path) == 0)
                {
                    // 암호화
                    foreach (var c in defaultValue)
                    {
                        sb.Append(Convert.ToByte(c).ToString("X2"));
                    }

                    WritePrivateProfileString(section, key, sb.ToString(), path);

                    return defaultValue;
                }

                // 복호화
                sb.Clear();
                for (int i = 0; i < str.Length / 2; i++)
                {
                    sb.Append((char)Convert.ToByte(str.ToString().Substring(i * 2, 2), 16));
                }
            }
            catch
            {
                Console.WriteLine(GetLastError());
            }

            return sb.ToString();
        }

        /// <summary>
        /// 암호화된 방식으로 기록합니다.
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public bool WritePassword(string section, string key, string value)
        {
            StringBuilder sb = new StringBuilder();

            try
            {
                // 암호화(ASCII to HEX)
                foreach (var c in value)
                {
                    sb.Append(Convert.ToByte(c).ToString("X2"));
                }

                WritePrivateProfileString(section, key, sb.ToString(), path);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[WARNING] {0}: {1}: {2}", GetType().Name, file_name, ex.Message);
                return false;
            }

            return true;
        }

        public bool ReadBool(string section, string key, bool defaultValue)
        {
            if (ReadString(section, key, (defaultValue) ? "1" : "0") == "1")
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool WriteBool(string section, string key, bool value)
        {
            try
            {
                WritePrivateProfileString(section, key, (value) ? "1" : "0", path);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[WARNING] {0}: {1}: {2}", GetType().Name, file_name, ex.Message);
                return false;
            }

            return true;
        }

        public int ReadInteger(string section, string key, int defaultValue)
        {
            try
            {
                return int.Parse(ReadString(section, key, defaultValue.ToString()));
            }
            catch
            {
                return defaultValue;
            }
        }

        public bool WriteInteger(string section, string key, int value)
        {
            try
            {
                WritePrivateProfileString(section, key, value.ToString(), path);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[WARNING] {0}: {1}: {2}", GetType().Name, file_name, ex.Message);
                return false;
            }

            return true;
        }

        public uint ReadUInteger(string section, string key, uint defaultValue)
        {
            try
            {
                return uint.Parse(ReadString(section, key, defaultValue.ToString()));
            }
            catch
            {
                return defaultValue;
            }
        }

        public bool WriteUInteger(string section, string key, uint value)
        {
            try
            {
                WritePrivateProfileString(section, key, value.ToString(), path);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[WARNING] {0}: {1}: {2}", GetType().Name, file_name, ex.Message);
                return false;
            }

            return true;
        }

        public float ReadFloat(string section, string key, float defaultValue)
        {
            try
            {
                return float.Parse(ReadString(section, key, defaultValue.ToString()));
            }
            catch
            {
                return defaultValue;
            }
        }

        public bool WriteFloat(string section, string key, float value)
        {
            try
            {
                WritePrivateProfileString(section, key, value.ToString(), path);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[WARNING] {0}: {1}: {2}", GetType().Name, file_name, ex.Message);
                return false;
            }

            return true;
        }

        public double ReadDouble(string section, string key, double defaultValue)
        {
            try
            {
                return double.Parse(ReadString(section, key, defaultValue.ToString()));
            }
            catch
            {
                return defaultValue;
            }
        }

        public bool WriteDouble(string section, string key, double value)
        {
            try
            {
                WritePrivateProfileString(section, key, value.ToString(), path);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[WARNING] {0}: {1}: {2}", GetType().Name, file_name, ex.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// 시간문자열을 읽습니다.
        /// </summary>
        /// <param name="section">섹션</param>
        /// <param name="key">키</param>
        /// <param name="defaultValue">기본값</param>
        /// <returns></returns>
        public DateTime ReadDateTime(string section, string key, DateTime defaultValue)
        {
            StringBuilder str = new StringBuilder(100, 8192);
            DateTime rtv = defaultValue;

            try
            {
                if (GetPrivateProfileString(section, key, string.Empty, str, str.MaxCapacity, path) == 0)
                {
                    WritePrivateProfileString(section, key, defaultValue.ToString("yyyy-MM-dd HH:mm:ss"), path);

                    return defaultValue;
                }

                rtv = Convert.ToDateTime(str.ToString());
            }
            catch
            {
                Console.WriteLine(GetLastError());
            }

            return rtv;
        }

        /// <summary>
        /// 시간문자열을 기록합니다.
        /// </summary>
        /// <param name="section">섹션</param>
        /// <param name="key">키</param>
        /// <param name="value">키에 해당하는 값</param>
        /// <returns>성공시: true, 실패시: false</returns>
        public bool WriteDateTime(string section, string key, DateTime value)
        {
            try
            {
                WritePrivateProfileString(section, key, value.ToString("yyyy-MM-dd HH:mm:ss"), path);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[WARNING] {0}: {1}: {2}", GetType().Name, file_name, ex.Message);
                return false;
            }

            return true;
        }
    }
}
