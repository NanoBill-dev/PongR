using UnityEngine;
using UnityEngine.SceneManagement;

namespace PongRoyale.App
{
    /// <summary>
    /// Composition root da aplicacao. Vive na cena Bootstrap, sobrevive as trocas de
    /// cena e sera o unico ponto que instancia e conecta os servicos do jogo
    /// (perfil, matchmaking, audio). Hoje so aplica as configuracoes de device e
    /// encaminha para a primeira cena real.
    ///
    /// Rodar SEMPRE a partir da Bootstrap garante que nenhum sistema dependa de
    /// "dar Play na cena certa".
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AppBootstrap : MonoBehaviour
    {
        [Header("Performance")]
        [SerializeField] private int targetFrameRate = 60;
        [SerializeField] private bool keepScreenAwake = true;

        [Header("Fluxo")]
        [SerializeField] private string firstSceneName = SceneNames.MainMenu;
        [SerializeField] private bool loadFirstSceneOnStart = true;

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            ApplyDeviceSettings();
        }

        private void Start()
        {
            if (loadFirstSceneOnStart)
            {
                SceneManager.LoadScene(firstSceneName);
            }
        }

        private void ApplyDeviceSettings()
        {
            // No mobile o vSync sobrepoe targetFrameRate, entao precisa ser desligado
            // para o cap de 60 fps valer de fato.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFrameRate;
            Screen.sleepTimeout = keepScreenAwake ? SleepTimeout.NeverSleep : SleepTimeout.SystemSetting;
        }
    }
}
