using System;
using Assets.Api;
using BepInEx;
using System.Timers;
using Assets.Features.Hud;
using BepInEx.Configuration;
using UnityEngine;
using aag.Natives.Api;
using System.Collections.Generic;
using Assets.Features.Core;


namespace LES
{
    [BepInProcess("Legion TD 2.exe")]
    [BepInPlugin("d8784b16-df1f-4dbb-ae2d-42f1355cb3a2", "LES", "1.0.0")]
    public class Plugin : BaseUnityPlugin
    {
        public readonly Timer timer = new();
        public GameObject soundMaster;

        public ConfigEntry<bool> configRubyOnLaunch;
        public static ConfigEntry<bool> configSoundsWhileSpectating;

        private readonly Queue<float> workerQueueEventTimes = new();

        public void Awake()
        {
            Console.WriteLine("Awake called for LES plugin");
            soundMaster = new GameObject("SoundMaster");
            DontDestroyOnLoad(soundMaster);
            configRubyOnLaunch = Config.Bind("Memes", "Ruby on launch", true, "Shout Ruby Ruben after 20 seconds of launching the game");
            configSoundsWhileSpectating = Config.Bind("Memes", "Play Sounds while Spectating", true, "Play sounds while spectating. If false, only play sounds while playing the game.");

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

            timer.Elapsed += Init;
            timer.Enabled = true;

            SoundEffect effect = soundMaster.AddComponent<SoundEffect>();


            soundMaster.GetComponent<SoundEffect>().Initialize("ruby.mp3");
            StartCoroutine(effect.GetAudioClip(SoundEffect.GetFileInfoFromName("ruby.mp3")));
            soundMaster.GetComponent<SoundEffect>().Initialize("fucked.mp3");
            StartCoroutine(effect.GetAudioClip(SoundEffect.GetFileInfoFromName("fucked.mp3")));
            soundMaster.GetComponent<SoundEffect>().Initialize("puuush.mp3");
            StartCoroutine(effect.GetAudioClip(SoundEffect.GetFileInfoFromName("puuush.mp3")));
            soundMaster.GetComponent<SoundEffect>().Initialize("DTon3.mp3");
            StartCoroutine(effect.GetAudioClip(SoundEffect.GetFileInfoFromName("DTon3.mp3")));
            soundMaster.GetComponent<SoundEffect>().Initialize("abortion.mp3");
            StartCoroutine(effect.GetAudioClip(SoundEffect.GetFileInfoFromName("abortion.mp3")));
            soundMaster.GetComponent<SoundEffect>().Initialize("fuck-you.mp3");
            StartCoroutine(effect.GetAudioClip(SoundEffect.GetFileInfoFromName("fuck-you.mp3")));

            Logger.LogInfo($"Plugin LES is loaded!");
        }

        public void WorkerBoughtOrQueued()
        {
            while (workerQueueEventTimes.Count > 0)
            {
                if (workerQueueEventTimes.Peek() < Time.time - 10f)
                {
                    workerQueueEventTimes.Dequeue();
                }
                else
                {
                    break;
                }
            }
            workerQueueEventTimes.Enqueue(Time.time);
            Console.WriteLine("Workers pushed / queued last 10 seconds: " + workerQueueEventTimes.Count);
            if (workerQueueEventTimes.Count > 10)
            {
                Console.WriteLine("More than 10 workers were queued or bought in the last 10 seconds.");
                SoundEffect.Play("puuush.mp3", 300f);
            }
        }

        public void Init(object source, ElapsedEventArgs eArgs)
        {
            Console.WriteLine("init called for LES plugin");

            if (configRubyOnLaunch.Value)
            {
                SoundEffect.Play("ruby.mp3", 60f);
            }


            /*
            //Some interesting events that are not used right now, but might be useful in the future
            HudApi.OnQueueHireUnit += (player, unit, unitType) =>
            {
                Console.WriteLine("Mercenary was queued up but not yet hired. Player: " + player + ", Unit: " + unit + ", Type: " + unitType);
            };
            //Mercenary from any player
            UnitApi.OnAnyUnitHired += (player, unit, unitType) =>
            {
                Console.WriteLine("Any Unit was hired. Player: " + player + ", Unit: " + unit + ", Type: " + unitType);
            };
            //Mercenary from you
            UnitApi.OnUnitHired += (player, unit, unitType) =>
            {
                Console.WriteLine("Unit was hired. Player: " + player + ", Unit: " + unit + ", Type: " + unitType);
            };

            //???
            PlayerApi.OnUnitFinishesTraining += (unit, unitType) =>
            {
                Console.WriteLine("Unit finished training. Unit: " + unit + ", Type: " + unitType);
            };
            // Tower was placed - not fired when upgrading a tower
            PlayerApi.OnUnitBuilt += (player, unitType, point, facing) =>
            {
                Console.WriteLine("Unit was built. Player " + player + ", unitType: " + unitType);
            };
            // Tower was upgraded - not fired when placing a tower
            PlayerApi.OnUnitUpgraded += (player, unit, unitType, sourceUnitType) =>
            {
                Console.WriteLine("Unit was upgraded. Player: " + player + ", Unit: " + unit + ", Type: " + unitType + ", Source Type: " + sourceUnitType);
            };
            //???
            PlayerApi.OnUnitBought += (player, unit, unitType) =>
            {
                Console.WriteLine("Unit bought. Player: " + player + " Unit: " + unit + ", Type: " + unitType);
            };
            // Fired when a worker is starting to be trained. So if you queue 10 workers, this will fire like once every 2-3 seconds
            PlayerApi.OnUnitStartsTraining += (unit, unitType) =>
            {
                Console.WriteLine("Unit started training. Unit: " + unit + ", Type: " + unitType);
            };
            UnitApi.OnUnitKilled += (unit, killer, bounty, bountyReceiver) =>
            {
                Console.WriteLine("Unit killed. Unit: " + unit + ", Killer: " + killer + ", Bounty: " + bounty + ", Bounty Receiver: " + bountyReceiver);
			};

            Rendering.OnUnitDies += (unit) =>
            {
                var type = aag.Natives.Snapshot.UnitProperties[unit].UnitType;
                if (type == "eggsack_unit_id")
                {
                    Console.WriteLine("Egg died, playing abortion.mp3");
                    SoundEffect.Play("abortion.mp3", 60f);
                }
            };

            // Fired when a worker is queued and paid for. This also fires when you just click a single worker, so there isn't really a queue
            // This event is NOT fired, when a queued, unpaid worker is now paid for.
            // So the number of workers one purchased is the number of times this event is fired PLUS HudApi.OnQueueTrainUnit count
            // HOWEVER, if someone has the gold to buy a worker, but shift click buys it, both events are fired
            PlayerApi.OnUnitAddedToTrainingQueue += (player, unit, unitType) =>
            {
                WorkerBoughtOrQueued();
            };
            // Fired when a worker is queued, but not yet paid for
            HudApi.OnQueueTrainUnit += (player, unit, unitType) =>
            {
                WorkerBoughtOrQueued();
            };

            HudApi.OnDisplayGameText += (header, content, duration, image) =>
            {
                if (content.Contains("cleared"))
                {
                    //Log("header " + header + " content: " + content);
                }
                if (content.Contains("leak"))
                {
                    Console.WriteLine("Somebody leaked. Here's the content: " + content);
                    var unitsLeakedMatch = System.Text.RegularExpressions.Regex.Matches(content, @"img\(Icons/([A-Za-z0-9_]+)\.png\)");
                    List<string> unitNames = [];
                    foreach (System.Text.RegularExpressions.Match match in unitsLeakedMatch)
                    {
                        unitNames.Add(match.Groups[1].Value);
                    }
                    Console.WriteLine("Units leaked: " + string.Join(", ", unitNames));
                    var waveNumber = HudApi.GetWaveNumber();
                    if (waveNumber == 3)
                    {
                        if (unitNames.Count == 1 && unitNames[0] == "DragonTurtle")
                        {
                            Console.WriteLine("Leaked a DT on 3.");
                            SoundEffect.Play("DTon3.mp3", 60f);
                            return; //make sure we don't play multiple sounds that overlap
                        }
                    }


                    //Extract the leak percentage from the content
                    var leakPercentageMatch = System.Text.RegularExpressions.Regex.Match(content, @"\((\d+)%\)");
                    int percentageInt = 0;
                    if (leakPercentageMatch.Success)
                    {
                        string percentage = leakPercentageMatch.Groups[1].Value; // "50"
                        percentageInt = int.Parse(percentage);
                    }
                    // |player(1) leaked |c(ff8800):(50%)|r
                    try
                    {
                        int player = int.Parse(content.Split(')')[0].Split('(')[1]);
                        string name = Scoreboard.GetName(ushort.Parse(player.ToString()));
                        Console.WriteLine("Player " + player + " leaked. Name: " + name);
                        if (name.Contains(":Ruben B"))
                        {
                            SoundEffect.Play("ruby.mp3", 60f);
                            return;
                        }
                        if (name.ToUpper().Contains(":DOOFUSMCDOOFACE"))
                        {
                            if (percentageInt >= 100)
                            {
                                Console.WriteLine("Doofus leaked 100% or more, trying to play sound effect.");
                                SoundEffect.Play("fuck-you.mp3", 600f);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Issues parsing a leak... :seenoevil:");
                        Console.WriteLine(ex.Message);
                        Console.WriteLine(ex.StackTrace);
                    }

                    if (leakPercentageMatch.Success)
                    {
                        if (percentageInt >= 100)
                        {
                            Console.WriteLine("Leak percentage is 100% or more, trying to play sound effect.");
                            SoundEffect.Play("fucked.mp3", 60f);
                        }
                        Console.WriteLine("Leak percentage: " + percentageInt);
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