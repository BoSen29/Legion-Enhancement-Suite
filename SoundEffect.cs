using UnityEngine;
using System;
using System.Linq;
using System.Reflection;
using Assets.Api;
using System.IO;
using UnityEngine.Networking;
using System.Collections;
using BepInEx.Logging;
using System.Collections.Generic;

namespace LES
{
    public class SoundEffect : MonoBehaviour
    {
        ManualLogSource Logger => BepInEx.Logging.Logger.CreateLogSource("LES.SoundEffect");
        public static readonly string basePath = AppDomain.CurrentDomain.BaseDirectory + "\\bepinex\\";

        public static Dictionary<string, AudioClip> soundEffects = [];
        public static Dictionary<string, float> soundTimeouts = [];

        public static void Play(string clipName, float timeout)
        {
            soundEffects.TryGetValue(clipName, out AudioClip clip);
            if (clip == null)
            {
                Console.WriteLine($"Sound effect {clipName} not found in soundEffects dictionary.");
                return;
            }

            Console.WriteLine($"Requested to play sound effect: {clipName}");
            float lastPlayed = soundTimeouts.TryGetValue(clipName, out float lastTime) ? lastTime : 0f;
            if (lastPlayed == 0f || Time.time - lastPlayed >= timeout)
            {
                SfxApi.PlayGlobalSound(clip);
                Console.WriteLine($"Playing sound effect: {clipName}, setting last played time to {Time.time}");
                soundTimeouts[clipName] = Time.time; // Update last played time
            }
            else
            {
                Console.WriteLine($"Sound effect {clipName} is on cooldown. Last played at {lastPlayed}, current time is {Time.time}");
            }
        }

        public static FileInfo GetFileInfoFromName(string fileName)
        {
            return new FileInfo(basePath + fileName);
        }

        public void Initialize(string fileName)
        {
            Console.WriteLine("Creating sound effect for " + fileName);
            FileInfo fileInfo = new(basePath + fileName);

            // Check if the audio file exists, if not, try to load it from embedded resources
            if (!fileInfo.Exists)
            {
                Console.WriteLine($"Sound effect file {fileName} does not exist at {fileInfo.FullName}. Attempting to load from embedded resources.");
                try
                {
                    string[] resources = Assembly.GetExecutingAssembly().GetManifestResourceNames();
                    string resource = resources.First(r => r.EndsWith(fileName));
                    if (resource != null)
                    {
                        Logger.LogInfo("Found this resource: " + resource);
                        using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource);
                        Logger.LogInfo("Opened readstream to read this badaboio for " + fileName);
                        using FileStream fileStream = fileInfo.OpenWrite();
                        stream.CopyTo(fileStream);
                        Console.WriteLine($"Sound effect file {fileName} copied to {fileInfo.FullName} from embedded resources.");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error loading sound effect from embedded resources: {e.Message}");
                    Logger.LogError(e.ToString());
                    Logger.LogError(e.StackTrace);
                }
            }
            else
            {
                Console.WriteLine($"Sound effect file {fileName} already exists at {fileInfo.FullName}.");
            }
        }

        
        public IEnumerator GetAudioClip(FileInfo fileInfo)
        {
            UnityWebRequest webRequest = UnityWebRequestMultimedia.GetAudioClip(
                fileInfo.FullName.ToString(), AudioType.MPEG);

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError)
            {
                Console.WriteLine("Error loading sound effect from " + fileInfo.FullName + ": " + webRequest.error);
                Debug.Log(webRequest.error);
            }
            else
            {
                Console.WriteLine("Successfully loaded sound effect from " + fileInfo.FullName);
                soundEffects[fileInfo.Name] = DownloadHandlerAudioClip.GetContent(webRequest);
            }
        }
    }
}