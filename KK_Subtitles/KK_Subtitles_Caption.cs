﻿using BepInEx.Logging;
using Harmony;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UniRx.Triggers;
using UnityEngine;
using UnityEngine.UI;
using Logger = BepInEx.Logger;

namespace KK_Subtitles
{
    public class Caption
    {
        public static GameObject Pane { get; internal set; }

        internal static void InitGUI()
        {
            if (!(Pane = Pane ?? GameObject.Find("KK_Subtitles_Caption")))
                Pane = new GameObject("KK_Subtitles_Caption");

            var cscl = Pane.GetComponent<CanvasScaler>() ?? Pane.AddComponent<CanvasScaler>();
            cscl.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cscl.referenceResolution = new Vector2(1920, 1080);
            cscl.matchWidthOrHeight = 0.5f;

            var canvas = Pane.GetComponent<Canvas>() ?? Pane.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            (Pane.GetComponent<CanvasGroup>() ?? Pane.AddComponent<CanvasGroup>()).blocksRaycasts = false;

            var vlg = Pane.GetComponent<VerticalLayoutGroup>() ?? Pane.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = vlg.childForceExpandHeight = vlg.childControlWidth = vlg.childForceExpandWidth = false;
            vlg.childAlignment = TextAnchor.LowerCenter;
            vlg.padding = new RectOffset(0, 0, 0, KK_Subtitles.textOffset.Value);
        }

        internal static void DisplayDialogueSubtitle(LoadVoice voice)
        {
            string text = "";
            FindText();
            void FindText()
            {
                foreach (var a in KK_Subtitles.ActionGameInfoInstance.dicTalkInfo)
                {
                    foreach (var b in a.Value)
                    {
                        foreach (var c in b.Value)
                        {
                            foreach (var d in c.Value.Where(x => x is ActionGame.Communication.Info.GenericInfo).Select(x => x as ActionGame.Communication.Info.GenericInfo))
                            {
                                text = d.talk.Where(x => x.assetbundle == voice.assetBundleName && x.file == voice.assetName).Select(x => x.text).FirstOrDefault();
                                if (!text.IsNullOrEmpty())
                                    return;
                            }
                        }
                    }
                }
            }
            if (text.IsNullOrEmpty())
                return;

            DisplaySubtitle(voice, text);
        }

        internal static void DisplayHSubtitle(LoadVoice voice)
        {
            List<HActionBase> lstProc = (List<HActionBase>)AccessTools.Field(typeof(HSceneProc), "lstProc").GetValue(KK_Subtitles.HSceneProcInstance);
            HActionBase mode = lstProc[(int)KK_Subtitles.HSceneProcInstance.flags.mode];
            HVoiceCtrl voicectrl = (HVoiceCtrl)AccessTools.Field(typeof(HActionBase), "voice").GetValue(mode);

            //At the start of the H scene, all the text was loaded. Look through the loaded stuff and find the text for the current spoken voice.
            string text = "";
            FindText();
            void FindText()
            {
                foreach (var a in voicectrl.dicVoiceIntos)
                {
                    foreach (var b in a)
                    {
                        foreach (var c in b.Value)
                        {
                            text = c.Value.Where(x => x.Value.pathAsset == voice.assetBundleName && x.Value.nameFile == voice.assetName).Select(x => x.Value.word).FirstOrDefault();
                            if (!text.IsNullOrEmpty())
                                return;
                        }
                    }
                }
            }
            if (text.IsNullOrEmpty())
                return;

            DisplaySubtitle(voice, text);
        }

        internal static void DisplayCharaMakerSubtitle(LoadVoice voice)
        {
            string subText = KK_Subtitles.CharaMakerSubs.Root.Elements("Sub").Where(x => (string)x.Attribute("Asset") == voice.assetName).Select(x => (string)x.Attribute("Text")).FirstOrDefault();
            DisplaySubtitle(voice, $"{subText}");
        }

        internal static void DisplaySubtitle(LoadVoice voice, string text, string speaker = "")
        {
            if (KK_Subtitles.showUntranslated.Value == false)
                foreach (var x in text.ToList())
                    if (JPChars.Contains(x))
                        return;

            Font fontFace = (Font)Resources.GetBuiltinResource(typeof(Font), $"{KK_Subtitles.fontName.Value}.ttf");
            int fsize = KK_Subtitles.fontSize.Value;
            fsize = (int)(fsize < 0 ? (fsize * Screen.height / -100.0) : fsize);

            GameObject subtitle = new GameObject(voice.assetName);
            subtitle.transform.SetParent(Pane.transform);

            var rect = subtitle.GetComponent<RectTransform>() ?? subtitle.AddComponent<RectTransform>();
            rect.pivot = new Vector2(0.5f, 0);
            rect.sizeDelta = new Vector2(Screen.width * 0.990f, fsize + (fsize * 0.05f));

            var subtitleText = subtitle.GetComponent<Text>() ?? subtitle.AddComponent<Text>();
            subtitleText.font = fontFace;
            subtitleText.fontSize = fsize;
            subtitleText.fontStyle = fontFace.dynamic ? KK_Subtitles.fontStyle.Value : FontStyle.Normal;
            subtitleText.alignment = KK_Subtitles.textAlign.Value;
            subtitleText.horizontalOverflow = HorizontalWrapMode.Wrap;
            subtitleText.verticalOverflow = VerticalWrapMode.Overflow;
            subtitleText.color = KK_Subtitles.textColor.Value;

            var subOutline = subtitle.GetComponent<Outline>() ?? subtitle.AddComponent<Outline>();
            subOutline.effectColor = KK_Subtitles.outlineColor.Value;
            subOutline.effectDistance = new Vector2(KK_Subtitles.outlineThickness.Value, KK_Subtitles.outlineThickness.Value);

            subtitleText.text = speaker.IsNullOrEmpty() ? text : $"{speaker}:{text}";

            Logger.Log(LogLevel.Debug, $"KK_Subtitles:{text}");

            voice.OnDestroyAsObservable().Subscribe(delegate (Unit _)
            {
                subtitle.transform.SetParent(null);
                Object.Destroy(subtitle);
            });
        }

        private static readonly HashSet<char> JPChars = new HashSet<char>("あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもやゆよらりるれろわんアイウエカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤユヨラリルレロワヲン");
    }
}