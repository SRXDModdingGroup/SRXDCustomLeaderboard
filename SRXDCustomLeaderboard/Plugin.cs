using System;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace SRXDCustomLeaderboard
{
    [BepInPlugin("SRXDCustomLeaderboard", "SRXDCustomLeaderboard", "0.0.1.0")]
    public class Plugin : BaseUnityPlugin
    {
        public static BepInEx.Logging.ManualLogSource Log;
        private void Awake()
        {
            // Plugin startup logic
            Log = Logger;
            Log.LogInfo($"Plugin SRXDCustomLeaderboard is loaded!");
            Harmony.CreateAndPatchAll(typeof(LeaderboardPatches));
        }

        private void Update()
        {
        }

        private class LeaderboardPatches
        {
            [HarmonyPatch(typeof(Player_Steam), nameof(Player_Steam.DownloadLeaderboard))]
            [HarmonyPostfix]
            static void dlLeaderboardPost(string id, LeaderboardResult leaderboardToFill) {
                Log.LogInfo($"{id}, {JsonUtility.ToJson(leaderboardToFill)}");
            }
             
            // look into  SubmitTrackLeaderboard on how metadata is created LeaderboardSubmissionMetaData
            [HarmonyPatch(typeof(Player_Steam), nameof(Player_Steam.SetLeaderboardValue))]
            [HarmonyPrefix]
            static void upLeaderboardPre(string key, LeaderboardSubmission submission) {
                Log.LogInfo($"{key}, {JsonUtility.ToJson(submission)}");
                // LeaderboardSubmissionMetaData: metadata field: [health, buildNumber, exeRevisionNumber, tracklistSize, _progress, score ^ 148089795, trackDataVersion, streak, fullComboState, tiebreakerScore]
            }
        }
    }
}