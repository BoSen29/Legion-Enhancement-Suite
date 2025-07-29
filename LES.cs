using System;
using Assets.Api;
using BepInEx;
using System.Timers;
using Assets.Features.Hud;
using BepInEx.Configuration;
using UnityEngine;
using System.IO;



/*
Events
Leaking: Leak message from OnDisplayGameText (images for the mercs that were received)
Building a unit: UnitAPI.On(Any)UnitHired
Queueing a worker (HudAPI)
Sending a mercenary: ??????
MiniScoreboard: Bottom left score board
*/
namespace LES
{
    [BepInProcess("Legion TD 2.exe")]
    [BepInPlugin("d8784b16-df1f-4dbb-ae2d-42f1355cb3a2", "LES", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public readonly Timer timer = new();
        public GameObject soundMaster;
    
        public ConfigEntry<bool> configRubyOnLaunch;

        public void Awake()
        {
            Console.WriteLine("Awake called for LES plugin");
            soundMaster = new GameObject("SoundMaster");
            DontDestroyOnLoad(soundMaster);
            configRubyOnLaunch = Config.Bind("Memes", "Ruby on launch", true, "Shout Ruby Ruben after 20 seconds of launching the game");

            try
            {
                Patch();
            }
            catch (Exception e)
            {
                Logger.LogError($"Error while injecting or patching: {e}");
                throw;
            }

            timer.Interval = 20000;

            timer.Elapsed += init;
            timer.Enabled = true;

            SoundEffect effect = soundMaster.AddComponent<SoundEffect>();

            FileInfo rubyFileInfo = SoundEffect.GetFileInfoFromName("ruby.mp3");
            FileInfo fuckedFileInfo = SoundEffect.GetFileInfoFromName("fucked.mp3");

            soundMaster.GetComponent<SoundEffect>().Initialize("ruby.mp3");
            StartCoroutine(effect.GetAudioClip(rubyFileInfo));
            soundMaster.GetComponent<SoundEffect>().Initialize("fucked.mp3");
            StartCoroutine(effect.GetAudioClip(fuckedFileInfo));

            Logger.LogInfo($"Plugin LES is loaded!");
        }

        public void init(object source, ElapsedEventArgs eArgs)
        {
            Console.WriteLine("init called for LES plugin");

            if (configRubyOnLaunch.Value)
            {
                SoundEffect.Play("ruby.mp3", 60f);
            }

            /*
            HudApi.OnQueueHireUnit += (ushort player, short unit, string unitType) =>
            {
                Console.WriteLine("Mercenary was queued up but not yet hired. Player: " + player + ", Unit: " + unit + ", Type: " + unitType);
            };
            UnitApi.OnAnyUnitHired += (ushort player, short unit, string unitType) =>
            {
                Console.WriteLine("Any Unit was hired. Player: " + player + ", Unit: " + unit + ", Type: " + unitType);
            };
            UnitApi.OnUnitHired += (ushort player, short unit, string unitType) =>
            {
                Console.WriteLine("Unit was hired. Player: " + player + ", Unit: " + unit + ", Type: " + unitType);
            };
            HudApi.OnQueueTrainUnit += (ushort player, short unit, string unitType) =>
            {
                Console.WriteLine("Worker was queued up Player: " + player + ", Unit: " + unit + ", Type: " + unitType);
            };
            */

            HudApi.OnDisplayGameText += (header, content, duration, image) =>
            {
                if (content.Contains("cleared"))
                {
                    //Log("header " + header + " content: " + content);
                }
                if (content.Contains("leak"))
                {
                    Console.WriteLine("Somebody leaked. Here's the content: " + content);
                    //Extract the leak percentage from the content
                    var match = System.Text.RegularExpressions.Regex.Match(content, @"\((\d+)%\)");
                    if (match.Success)
                    {
                        string percentage = match.Groups[1].Value; // "50"
                        int percentageInt = int.Parse(percentage);
                        if (percentageInt >= 100)
                        {
                            Console.WriteLine("Leak percentage is 100% or more, trying to play sound effect.");
                            SoundEffect.Play("fucked.mp3", 60f);
                        }
                        Console.WriteLine("Leak percentage: " + percentage);
                    }
                    // |player(1) leaked |c(ff8800):(50%)|r
                    try
                    {
                        int player = int.Parse(content.Split(')')[0].Split('(')[1]);
                        string name = Scoreboard.GetName(ushort.Parse(player.ToString()));
                        if (name.Contains(":Ruben B"))
                        {
                            SoundEffect.Play("ruby.mp3", 60f);
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Issues parsing a leak... :seenoevil:");
                        Console.WriteLine(ex.Message);
                        Console.WriteLine(ex.StackTrace);
                    }
                }
            };

            timer.Enabled = false;
        }

        public void OnDestroy()
        {
            UnPatch();
        }

        private void Patch()
        {
        }

        private void UnPatch()
        {
        }
    }
}