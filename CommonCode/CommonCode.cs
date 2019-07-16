﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using UnityEngine;

namespace CommonCode
{
    internal class CC
    {
        private static int _language = -1;
        /// <summary>
        /// Safely get the language as configured in setup.xml if it exists.
        /// </summary>
        public static int Language
        {
            get
            {
                if (_language == -1)
                {
                    try
                    {
                        var dataXml = XElement.Load("UserData/setup.xml");

                        if (dataXml != null)
                        {
                            IEnumerable<XElement> enumerable = dataXml.Elements();
                            foreach (XElement xelement in enumerable)
                            {
                                if (xelement.Name.ToString() == "Language")
                                {
                                    _language = int.Parse(xelement.Value);
                                    break;
                                }
                            }
                        }
                    }
                    catch
                    {
                        _language = 0;
                    }
                    finally
                    {
                        if (_language == -1)
                            _language = 0;
                    }
                }

                return _language;
            }
        }
        /// <summary>
        /// Open explorer focused on the specified file or directory
        /// </summary>
        internal static void OpenFileInExplorer(string filename)
        {
            if (filename == null)
                throw new ArgumentNullException(nameof(filename));

            try { NativeMethods.OpenFolderAndSelectFile(filename); }
            catch (Exception) { Process.Start("explorer.exe", $"/select, \"{filename}\""); }
        }
        internal static class NativeMethods
        {
            /// <summary>
            /// Open explorer focused on item. Reuses already opened explorer windows unlike Process.Start
            /// </summary>
            public static void OpenFolderAndSelectFile(string filename)
            {
                var pidl = ILCreateFromPathW(filename);
                SHOpenFolderAndSelectItems(pidl, 0, IntPtr.Zero, 0);
                ILFree(pidl);
            }

            [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
            private static extern IntPtr ILCreateFromPathW(string pszPath);

            [DllImport("shell32.dll")]
            private static extern int SHOpenFolderAndSelectItems(IntPtr pidlFolder, int cild, IntPtr apidl, int dwFlags);

            [DllImport("shell32.dll")]
            private static extern void ILFree(IntPtr pidl);
        }

        internal static void Log(string text) => BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Info, text);
        internal static void Log(BepInEx.Logging.LogLevel level, string text) => BepInEx.Logger.Log(level, text);
        internal static void Log(object text) => BepInEx.Logger.Log(BepInEx.Logging.LogLevel.Info, text?.ToString());
        internal static void Log(BepInEx.Logging.LogLevel level, object text) => BepInEx.Logger.Log(level, text?.ToString());
        internal static void StackTrace() => Log(new System.Diagnostics.StackTrace());

        internal static class Paths
        {
            internal static readonly string FemaleCardPath = Path.Combine(UserData.Path, "chara/female/");
            internal static readonly string MaleCardPath = Path.Combine(UserData.Path, "chara/male/");
            internal static readonly string CoordinateCardPath = Path.Combine(UserData.Path, "coordinate/");
        }
    }

    internal static class Extensions
    {
        private static readonly System.Random rng = new System.Random();
        public static void Randomize<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
        public static string NameFormatted(this GameObject go) => go.name.Replace("(Instance)", "").Trim();
        public static string NameFormatted(this Material go) => go.name.Replace("(Instance)", "").Trim();
        public static string NameFormatted(this Renderer go) => go.name.Replace("(Instance)", "").Trim();
        public static string NameFormatted(this Shader go) => go.name.Replace("(Instance)", "").Trim();
    }
}
