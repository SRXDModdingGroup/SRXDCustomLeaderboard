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
    [BepInPlugin("SRXD.CustomLeaderboard", "SRXDCustomLeaderboard", "0.0.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        private static ConfigEntry<bool> enabled;
        private static ConfigEntry<bool> enabledSteam;
        private static ConfigEntry<string> baseUri;
        private static ConfigEntry<string> authCookie;
        
        private static BepInEx.Logging.ManualLogSource Log;

        private static LeaderboardResult leaderboard;
        
        private void Awake()
        {
            // Plugin startup logic
            Log = Logger;
            Log.LogInfo($"Plugin SRXDCustomLeaderboard is loaded!");
            
            baseUri = Config.Bind(
                "General", 
                "LeaderboardServerUri",
                "https://srxdcustomleaderboard.vercel.app/api/leaderboard",
                "Custom Leaderboards Server Uri"
            );
            authCookie = Config.Bind(
                "General", 
                "LeaderboardServerAuthCookie",
                "CLIENT-enterTokenHere",
                "Custom Leaderboards Server Authentication Cookie"
            );
            enabled = Config.Bind(
                "General.Toggles", 
                "EnableCustomLeaderboard",
                true,
                "Enable Submitting to Custom Leaderboards"
            );
            enabledSteam = Config.Bind(
                "General.Toggles", 
                "EnableSteamLeaderboard",
                true,
                "Enable Submitting to Steam Leaderboards"
            );

            
            Harmony.CreateAndPatchAll(typeof(LeaderboardPatches));

            if (authCookie.Value == authCookie.DefaultValue.ToString())
            {
                Log.LogWarning("The LeaderboardServerAuthCookie is invalid, please visit https://srxdcustomleaderboard.vercel.app/ to create one.");
            }
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
            [HarmonyPatch(typeof(GameStateManager), "ProcessCommandTokenWithParam")]
            [HarmonyPrefix]
            static void processTokenPre(string token, string paramToken) {
                if (token == "token") {
                    authCookie.Value = paramToken;
                }
            }
            
            // [HarmonyPatch(typeof(PlayableTrackData), "GenerateNoteHash")]
            // [HarmonyPrefix]
            // static void generateNoteHashPre(PlayableTrackData __instance) {
            //     Log.LogInfo($"JSON: {__instance.jsonFileToHash}");
            // }
        }
    }
}