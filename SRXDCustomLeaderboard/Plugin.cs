using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Net;
using System.Threading.Tasks;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Networking;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading;

namespace SRXDCustomLeaderboad {
    [BepInPlugin("SRXDCustomLeaderboard", "SRXDCustomLeaderboard", "0.0.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static ConfigEntry<bool> enabled;
        public static ConfigEntry<bool> enabledSteam;
        public static ConfigEntry<string> baseUri;
        public static ConfigEntry<string> authCookie;
        
        public static BepInEx.Logging.ManualLogSource Log;

        public static LeaderboardResult leaderboard;
        
        private void Awake()
        {
            // Plugin startup logic
            Log = Logger;
            Log.LogInfo($"Plugin SRXDCustomLeaderboard is loaded!");
            
            baseUri = Config.Bind(
                "General", 
                "LeaderboardsServerUri",
                "http://192.168.0.14:3000/api/leaderboard",
                "Custom Leaderboards Server Uri"
            );
            authCookie = Config.Bind(
                "General", 
                "LeaderboardsServerAuthCookie",
                "CLIENT-b6acc9e7e6b1f940e09d33125c494ff5",
                "Custom Leaderboards Server Authentication Cookie"
            );
            enabled = Config.Bind(
                "General.Toggles", 
                "EnableCustomLeaderboards",
                true,
                "Enable Submitting to Custom Leaderboards"
            );
            enabledSteam = Config.Bind(
                "General.Toggles", 
                "EnableSteamLeaderboards",
                true,
                "Enable Submitting to Steam Leaderboards"
            );

            Harmony.CreateAndPatchAll(typeof(LeaderboardPatches));
        }

        private class LeaderboardPatches
        {
            [HarmonyPatch(typeof(Player_Steam), nameof(Player_Steam.DownloadLeaderboard))]
            [HarmonyPrefix]
            static bool dlLeaderboardPre(string id, ref LeaderboardResult leaderboardToFill)
            {
                leaderboard = leaderboardToFill;
                
                Post("/submission/get", JsonUtility.ToJson(leaderboardToFill)).ContinueWith(e =>
                {
                    JsonUtility.FromJsonOverwrite(e.Result, leaderboard);
                    leaderboard.State = LeaderboardResult.RequestState.Success;
                });
                
                return !enabled.Value;
            }
             
            // look into  SubmitTrackLeaderboard on how metadata is created LeaderboardSubmissionMetaData
            [HarmonyPatch(typeof(Player_Steam), nameof(Player_Steam.SetLeaderboardValue))]
            [HarmonyPrefix]
            static bool upLeaderboardPre(string key, LeaderboardSubmission submission) {
                if (enabled.Value)
                {
                    Log.LogInfo($"{key}, {JsonUtility.ToJson(submission)}");
                    Task.Run(async () => await Post("/submission/submit", JsonUtility.ToJson(submission)));
                    // LeaderboardSubmissionMetaData: metadata field: [health, buildNumber, exeRevisionNumber, tracklistSize, _progress, score ^ 148089795, trackDataVersion, streak, fullComboState, tiebreakerScore]
                }
                return enabledSteam.Value;
            }
            
            static async Task<string> Post(string path, string body)
            {   
                using (var handler = new HttpClientHandler() { UseCookies = false })
                using (var client = new HttpClient(handler))
                {
                    client.DefaultRequestHeaders.Add("Cookie", $"next-auth.session-token={authCookie.Value}");
                    var content = new StringContent(body, Encoding.UTF8, "application/json");
                    var result = await client.PostAsync($"{baseUri.Value}{path}", content);
                    string resultContent = await result.Content.ReadAsStringAsync();
                    Log.LogInfo(resultContent);
                    return resultContent;
                }
            }
        }
    }
}