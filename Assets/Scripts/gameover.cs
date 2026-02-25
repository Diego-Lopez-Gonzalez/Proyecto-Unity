using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class GameOverManager : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────
    public static GameOverManager Instancia { get; private set; }

    // ── Inspector ────────────────────────────────────────────────
    [Header("UI · Panel compartido")]
    [Tooltip("Panel raíz (desactivado al inicio).")]
    [SerializeField] private GameObject panel;

    [Tooltip("Texto principal del panel.")]
    [SerializeField] private Text textoMensaje;

    [Tooltip("Texto secundario / subtítulo (opcional).")]
    [SerializeField] private Text textoSubtitulo;

    [Header("Mensajes · Game Over")]
    [SerializeField] private string mensajeDerrota   = "¡TE HAN ATRAPADO!";
    [SerializeField] private string subtituloDerrota = "No has conseguido escapar...";

    [Header("Mensajes · Victoria")]
    [SerializeField] private string mensajeVictoria   = "¡HAS ESCAPADO!";
    [SerializeField] private string subtituloVictoria = "Has robado todo lo que necesitabas.";

    [Header("Tiempo bala (solo derrota)")]
    [SerializeField] [Range(0.01f, 1f)] private float escalaRalentizada    = 0.15f;
    [SerializeField]                    private float duracionRalentizacion = 0.6f;

    [Header("Pausa antes de mostrar el panel")]
    [Tooltip("Tiempo real (segundos) antes de mostrar el panel en ambos casos.")]
    [SerializeField] private float pausaAntesDePanel = 0.8f;

    [Header("Escena")]
    [Tooltip("Nombre de la escena al reiniciar (vacío = escena actual).")]
    [SerializeField] private string escenaReinicio = "";

    // ── Estado interno ───────────────────────────────────────────
    private bool juegoTerminado = false;

    // ── Ciclo de vida ────────────────────────────────────────────
    private void Awake()
    {
        if (Instancia != null && Instancia != this) { Destroy(gameObject); return; }
        Instancia = this;
    }

    private void Start()
    {
        if (panel != null) panel.SetActive(false);
    }

    // ── API pública ──────────────────────────────────────────────

    /// <summary>Llama a este método cuando el guardia atrapa al jugador.</summary>
    public void ActivarGameOver()
    {
        if (juegoTerminado) return;
        juegoTerminado = true;
        Debug.Log("[GameOverManager] DERROTA");
        StartCoroutine(SecuenciaDerrota());
    }

    /// <summary>Llama a este método cuando el jugador escapa con todos los objetos.</summary>
    public void ActivarVictoria()
    {
        if (juegoTerminado) return;
        juegoTerminado = true;
        Debug.Log("[GameOverManager] VICTORIA");
        StartCoroutine(SecuenciaVictoria());
    }

    /// <summary>Conecta este método al botón del panel.</summary>
    public void Reiniciar()
    {
        Time.timeScale      = 1f;
        Time.fixedDeltaTime = 0.02f;
        string escena = string.IsNullOrEmpty(escenaReinicio)
            ? SceneManager.GetActiveScene().name
            : escenaReinicio;
        SceneManager.LoadScene(escena);
    }

    // ── Secuencias ───────────────────────────────────────────────

    // Derrota: tiempo bala → pausa → panel
    private IEnumerator SecuenciaDerrota()
    {
        yield return StartCoroutine(RalentizarTiempo(escalaRalentizada, duracionRalentizacion));
        yield return new WaitForSecondsRealtime(pausaAntesDePanel);
        Time.timeScale = 0f;
        MostrarPanel(mensajeDerrota, subtituloDerrota);
    }

    // Victoria: sin ralentización, pausa breve → panel
    private IEnumerator SecuenciaVictoria()
    {
        yield return new WaitForSecondsRealtime(pausaAntesDePanel);
        Time.timeScale = 0f;
        MostrarPanel(mensajeVictoria, subtituloVictoria);
    }

    // ── Helpers ──────────────────────────────────────────────────

    private IEnumerator RalentizarTiempo(float escalaObjetivo, float duracionReal)
    {
        float escalaInicial = Time.timeScale;
        float transcurrido  = 0f;

        while (transcurrido < duracionReal)
        {
            transcurrido        += Time.unscaledDeltaTime;
            Time.timeScale       = Mathf.Lerp(escalaInicial, escalaObjetivo, transcurrido / duracionReal);
            Time.fixedDeltaTime  = 0.02f * Time.timeScale;
            yield return null;
        }

        Time.timeScale      = escalaObjetivo;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }

    private void MostrarPanel(string mensaje, string subtitulo)
    {
        if (panel == null) { Debug.LogWarning("[GameOverManager] Panel no asignado."); return; }

        if (textoMensaje   != null) textoMensaje.text   = mensaje;
        if (textoSubtitulo != null) textoSubtitulo.text = subtitulo;

        panel.SetActive(true);
    }
}