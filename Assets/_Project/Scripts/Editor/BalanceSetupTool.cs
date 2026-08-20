using System.IO;
using PongRoyale.Gameplay.Balance;
using UnityEditor;
using UnityEngine;

namespace PongRoyale.Editor
{
    /// <summary>
    /// Cria o asset de balanceamento padrao do projeto. Idempotente: se o asset ja
    /// existe, preserva os valores que voce ajustou em vez de sobrescrever.
    /// Menu: Pong Royale / Setup / Create Default Balance Asset
    /// </summary>
    public static class BalanceSetupTool
    {
        private const string BalanceFolder = "Assets/_Project/ScriptableObjects/Balance";
        private const string DefaultAssetPath = BalanceFolder + "/DefaultBalance.asset";

        [MenuItem("Pong Royale/Setup/Create Default Balance Asset")]
        public static void CreateDefaultBalanceAsset()
        {
            if (File.Exists(DefaultAssetPath))
            {
                Debug.Log($"[BalanceSetup] '{DefaultAssetPath}' ja existe, valores preservados.");
                return;
            }

            Directory.CreateDirectory(BalanceFolder);

            var asset = ScriptableObject.CreateInstance<BalanceData>();
            AssetDatabase.CreateAsset(asset, DefaultAssetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[BalanceSetup] Asset de balanceamento criado em {DefaultAssetPath}.");
        }
    }
}
