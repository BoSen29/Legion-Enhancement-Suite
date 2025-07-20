using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Assets.Api;
using BepInEx;
using HarmonyLib;
using System.Timers;
using Assets.Features.Hud;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using BepInEx.Configuration;
using System.Data;

namespace LES
{
    using P = Plugin;

    [BepInProcess("Legion TD 2.exe")]
    [BepInPlugin("d8784b16-df1f-4dbb-ae2d-42f1355cb3a2", "LES", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        private Timer timer = new Timer();
        private AudioClip ruby;
        private AudioClip fucked;
        private ConfigEntry<bool> configRubyOnLaunch;

        public void Awake() {
            configRubyOnLaunch = Config.Bind("Memes", "Ruby on launch", true, "Shout Ruby Ruben after 20 seconds of launching the game");

            try {
                Patch();
            }
            catch (Exception e) {
                Logger.LogError($"Error while injecting or patching: {e}");
                throw;
            }

            this.timer.Interval = 20000;

            this.timer.Elapsed += init;
            this.timer.Enabled = true;

            string basePath = AppDomain.CurrentDomain.BaseDirectory;
            FileInfo rubyPath = new FileInfo(basePath + $@"\bepinex\ruby.mp3");
            FileInfo fuckedPath = new FileInfo(basePath + $@"\bepinex\fucked.mp3");

            List<FileInfo> files = new List<FileInfo>();
            files.Add(rubyPath);
            files.Add(fuckedPath);

            foreach (FileInfo file in files)
            {
                if (!file.Exists)
                {
                    String fileName = Path.GetFileName(file.Name);
                    try
                    {
                        string[] resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();
                        string resource = resources.First(r => r.EndsWith(fileName));
                        if (resource != null)
                        {
                            Logger.LogInfo("Found this resource: " + resource);
                            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
                            {
                                Logger.LogInfo("Opened readstream to read this badaboio");
                                using (FileStream fileStream = file.OpenWrite())
                                {
                                    stream.CopyTo(fileStream);
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.LogError(e.ToString());
                        Logger.LogError(e.StackTrace);
                    }
                }
            }

            StartCoroutine(GetAudioClip(rubyPath));
            Logger.LogInfo("Attempting to load mp3 file " + rubyPath.ToString());

            StartCoroutine(GetAudioClip(fuckedPath));
            Logger.LogInfo("Attempting to load mp3 file " + fuckedPath.ToString());

            Logger.LogInfo($"Plugin LES is loaded!");
        }

        public void init(object source, System.Timers.ElapsedEventArgs eArgs)
        {
            if (this.ruby != null && configRubyOnLaunch.Value)
            {
                SfxApi.PlayGlobalSound(this.ruby);
            }

            HudApi.OnDisplayGameText += (string header, string content, float duration, string image) =>
            {
                if (content.Contains("cleared"))
                {
                    //Log("header " + header + " content: " + content);
                }
                if (content.Contains("leak"))
                {
                    Console.WriteLine("Somebody leaked. Here's the content: " + content);
                    var match = System.Text.RegularExpressions.Regex.Match(content, @"\((\d+)%\)");
                    if (match.Success)
                    {
                        string percentage = match.Groups[1].Value; // "50"
                        int percentageInt = int.Parse(percentage);
                        if(percentageInt >= 100 && this.fucked != null)
                        {
                            SfxApi.PlayGlobalSound(this.fucked);
                        }
                        Console.WriteLine("Leak percentage: " + percentage);
                    }
                    // |player(1) leaked |c(ff8800):(50%)|r
                    try
                    {
                        int player = int.Parse(content.Split(')')[0].Split('(')[1]);
                        string name = Scoreboard.GetName(ushort.Parse(player.ToString()));
                        if (name.Contains(":Ruben B") && this.ruby != null)
                        {
                            SfxApi.PlayGlobalSound(this.ruby);
                        }
                        else
                        {
                            Console.WriteLine("Ruby ruben: Player leaked, but it wasn't ruby ruben :sad " + name);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Issues parsing a leak... :seenoevil:");
                        Console.WriteLine(ex.Message);
                        Console.WriteLine(ex.StackTrace);
                    }
                    Console.WriteLine("Leak registered!");
                    Console.WriteLine(content);


                }
            };

            this.timer.Enabled = false;
        }

        IEnumerator GetAudioClip(FileInfo file)
        {
            UnityWebRequest webRequest = UnityWebRequestMultimedia.GetAudioClip(
                file.FullName.ToString(), AudioType.MPEG);

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log(webRequest.error);
            }
            else
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(webRequest);
                clip.name = file.Name;
                if(file.Name.Contains("fucked"))
                    this.fucked = clip;
                else if (file.Name.Contains("ruby"))
                    this.ruby = clip;
            }
        }
        public void OnDestroy() {
            UnPatch();
        }

        private void Patch() {
        }

        private void UnPatch() {
        }
    }
}