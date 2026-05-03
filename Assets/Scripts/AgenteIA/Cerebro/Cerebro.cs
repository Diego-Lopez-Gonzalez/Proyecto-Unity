using System.Collections.Generic;
using UnityEngine;

namespace GuardiaIA
{
    public class Cerebro : MonoBehaviour, IAgente
    {
        [Header("Visión")]
        [SerializeField] private float rangoVision       = 10f;
        [SerializeField] private float anguloVision      = 90f;
        [SerializeField] private LayerMask capasObstaculo;

        [Header("Jugador")]
        [SerializeField] private Transform jugador;

        [Header("Objeto vigilado")]
        [SerializeField] private Transform objetoVigilado;

        [Header("Palanca y ruta")]
        [SerializeField] private Transform   palanca;
        [SerializeField] private Transform[] rutaTrasPalanca;

        [Header("Patrulla")]
        [SerializeField] private Transform[] rutaPatrulla;
        [SerializeField] private float tiempoEsperaEnPunto = 2f;

        [Header("Velocidades")]
        [SerializeField] private float velocidadPatrulla    = 2f;
        [SerializeField] private float velocidadPersecucion = 5f;

        [Header("Búsqueda")]
        [SerializeField] private float tiempoBusqueda = 3f;
        [SerializeField] private float radioBusqueda  = 4f;

        [Header("Sonido")]
        [SerializeField] private float radioEscucha        = 12f;
        [SerializeField] private float radioIncertidumbre  = 3f;

        [Header("Captura")]
        [SerializeField] private float radioCaptura          = 1.2f;
        [SerializeField] private float rangoDeteccionPuertas = 3f;

        [Header("Puntos de cierre")]
        [SerializeField] private List<Transform> puntosCorte = new List<Transform>();

        [Header("Barrido")]
        [SerializeField] private List<Transform> puntosBarrido = new List<Transform>();

        private BaseConocimiento   conocimiento;
        private IEstado            estadoActual;
        private Acciones           acciones;
        private SensorVision       sensorVision;
        private SensorTacto        sensorTacto;
        private SensorSonido       sensorSonido;
        private GestorComunicacion gestorComunicacion;

        public SensorTacto         SensorTacto        => sensorTacto;
        public GestorComunicacion  GestorComunicacion => gestorComunicacion;

        // ── IAgente ───────────────────────────────────────────────────────────

        public bool EstaOcupado =>
            estadoActual is EstadoPersecucion  ||
            estadoActual is EstadoCerrarZona   ||
            estadoActual is EstadoYendoPalanca ||
            estadoActual is EstadoBarrerMapa;

        public PrioridadContrato PrioridadTareaActual =>
            conocimiento?.PrioridadTareaActual ?? PrioridadContrato.Baja;

        public void InterrumpirTareaActual()
        {
            Debug.Log($"[Cerebro] {name} interrumpe tarea activa " +
                      $"(conv:{conocimiento.ConversationIdTarea}) por contrato de mayor prioridad.");

            if (conocimiento.ConversationIdTarea != null)
                gestorComunicacion?.NotificarTareaFallida(conocimiento.ConversationIdTarea);

            conocimiento.TareaAsignada        = TareaContrato.Ninguna;
            conocimiento.ConversationIdTarea  = null;
            conocimiento.PrioridadTareaActual = PrioridadContrato.Baja;

            CambiarEstado(new EstadoPatrulla());
        }

        // ── Ciclo de vida ─────────────────────────────────────────────────────

        private void Start()
        {
            conocimiento = new BaseConocimiento
            {
                Palanca              = palanca,
                RutaTrasPalanca      = rutaTrasPalanca,
                RutaPatrulla         = rutaPatrulla,
                IndicePatrullaActual = 0,
                VelocidadPatrulla    = velocidadPatrulla,
                VelocidadPersecucion = velocidadPersecucion,
                TiempoEsperaEnPunto  = tiempoEsperaEnPunto,
                TiempoBusqueda       = tiempoBusqueda,
                RadioBusqueda        = radioBusqueda,
                PuntosCorte          = puntosCorte,
                PuntosBarrido        = puntosBarrido
            };

            acciones           = GetComponent<Acciones>();
            sensorVision       = GetComponent<SensorVision>();
            sensorTacto        = GetComponent<SensorTacto>();
            sensorSonido       = GetComponent<SensorSonido>();
            gestorComunicacion = GetComponent<GestorComunicacion>();

            sensorVision.Inicializar(this, jugador, objetoVigilado,
                                     rangoVision, anguloVision, capasObstaculo);
            sensorTacto.Inicializar(this, jugador, radioCaptura, rangoDeteccionPuertas);
            sensorSonido.Inicializar(this, radioEscucha, radioIncertidumbre);

            CambiarEstado(new EstadoPatrulla());
        }

        private void Update()
        {
            estadoActual?.Ejecutar(this, conocimiento, acciones);

            if (estadoActual != null && estadoActual.HaTerminado)
                EvaluarPrioridades();
        }

        // ── Prioridades ───────────────────────────────────────────────────────

        private void EvaluarPrioridades()
        {
            // P1: jugador visible → perseguir siempre.
            if (conocimiento.JugadorVisible)
            {
                if (estadoActual is EstadoYendoPalanca)
                    conocimiento.PalancaPendienteTrasPerder = true;

                conocimiento.TareaAsignada        = TareaContrato.Ninguna;
                conocimiento.PrioridadTareaActual = PrioridadContrato.Baja;
                CambiarEstado(new EstadoPersecucion());
                return;
            }

            // P2: sonido pendiente → investigar.
            if (conocimiento.SonidoPendiente)
            {
                conocimiento.SonidoPendiente = false;
                CambiarEstado(new EstadoInvestigarSonido());
                return;
            }

            // P3: objeto robado, palanca no gestionada → ir a palanca.
            if (conocimiento.ObjetoDesaparecido && !conocimiento.PalancaYaGestionada)
            {
                CambiarEstado(new EstadoYendoPalanca());
                return;
            }

            // P4: tarea de contrato → cerrar zona.
            if (conocimiento.TareaAsignada == TareaContrato.CerrarZona)
            {
                CambiarEstado(new EstadoCerrarZona(conocimiento.ConversationIdTarea));
                return;
            }

            // P5: tarea de contrato → ir a palanca.
            if (conocimiento.TareaAsignada == TareaContrato.IrAPalanca)
            {
                CambiarEstado(new EstadoYendoPalanca(conocimiento.ConversationIdTarea));
                return;
            }

            // P5b: tarea de contrato → perseguir.
            if (conocimiento.TareaAsignada == TareaContrato.Perseguir)
            {
                CambiarEstado(new EstadoPersecucion());
                return;
            }

            // P5c: tarea de contrato → barrer mapa.
            if (conocimiento.TareaAsignada == TareaContrato.BarrerMapa)
            {
                CambiarEstado(new EstadoBarrerMapa(conocimiento.ConversationIdTarea));
                return;
            }

            // P6: objeto robado, palanca ya gestionada → inspección.
            if (conocimiento.ObjetoDesaparecido && conocimiento.PalancaYaGestionada)
            {
                CambiarEstado(new EstadoInspeccionAleatoria());
                return;
            }

            // P7 (base): patrullar.
            CambiarEstado(new EstadoPatrulla());
        }

        // ── Callbacks de visión ───────────────────────────────────────────────

        public void OnJugadorDetectado(Vector3 posicion)
        {
            conocimiento.JugadorVisible        = true;
            conocimiento.UltimaPosicionJugador = posicion;

            const PrioridadContrato prioridad = PrioridadContrato.Alta;
            var tareas = Planificador.PlanParaEvento(EventoSeguridad.JugadorDetectado, prioridad);
            gestorComunicacion?.IniciarContractNet(posicion, tareas, prioridad);

            EvaluarPrioridades();
        }

        public void OnActualizarPosicionJugador(Vector3 posicion)
        {
            conocimiento.UltimaPosicionJugador = posicion;
        }

        public void OnJugadorPerdido()
        {
            conocimiento.JugadorVisible = false;

            if (conocimiento.PalancaPendienteTrasPerder)
            {
                conocimiento.PalancaPendienteTrasPerder = false;
                CambiarEstado(new EstadoYendoPalanca());
            }
            else
            {
                CambiarEstado(new EstadoBusqueda());
            }
        }

        public void OnObjetoDesaparecido()
        {
            conocimiento.ObjetoDesaparecido = true;

            const PrioridadContrato prioridad = PrioridadContrato.Media;
            var tareas = Planificador.PlanParaEvento(EventoSeguridad.ObjetoRobado, prioridad);
            gestorComunicacion?.IniciarContractNet(
                conocimiento.UltimaPosicionJugador, tareas, prioridad);

            EvaluarPrioridades();
        }

        public void OnSonidoDetectado(Vector3 posicionPercibida, float radioDesvio)
        {
            conocimiento.PosicionPercibidaSonido  = posicionPercibida;
            conocimiento.RadioIncertidumbreSonido = radioDesvio;
            conocimiento.SonidoPendiente          = true;
            EvaluarPrioridades();
        }

        public void OnPalancaGestionada()
        {
            conocimiento.PalancaYaGestionada = true;
        }

        public void OnRevisarPalanca()
        {
            conocimiento.PalancaYaGestionada = false;
        }

        // ── Callbacks de contrato ─────────────────────────────────────────────

        public void OnTareaAsignada(TareaContrato tarea, string conversationId)
        {
            Debug.Log($"[Cerebro] {name} tarea asignada: {tarea} conv:{conversationId}");
            conocimiento.TareaAsignada        = tarea;
            conocimiento.ConversationIdTarea  = conversationId;
            EvaluarPrioridades();
        }

        public void OnTareaAsignada(TareaContrato tarea, string conversationId, PrioridadContrato prioridad)
        {
            Debug.Log($"[Cerebro] {name} tarea asignada: {tarea} [{prioridad}] conv:{conversationId}");
            conocimiento.TareaAsignada        = tarea;
            conocimiento.ConversationIdTarea  = conversationId;
            conocimiento.PrioridadTareaActual = prioridad;
            EvaluarPrioridades();
        }

        public void OnTareaCancelada(string conversationId)
        {
            Debug.Log($"[Cerebro] {name} tarea cancelada conv:{conversationId}");
            conocimiento.TareaAsignada        = TareaContrato.Ninguna;
            conocimiento.ConversationIdTarea  = null;
            conocimiento.PrioridadTareaActual = PrioridadContrato.Baja;
            EvaluarPrioridades();
        }

        // ── Otros callbacks ───────────────────────────────────────────────────

        public void OnJugadorCapturado()
        {
            Debug.Log("[Cerebro] ¡JUGADOR ATRAPADO! → GAME OVER");
            GameOverManager.Instancia.ActivarGameOver();
        }

        public void OnPuertaDetectada(PuertaNavMesh puerta)
        {
            puerta.Abrir();
        }

        // ── Gestión de estados ────────────────────────────────────────────────

        public void CambiarEstado(IEstado nuevoEstado)
        {
            if (estadoActual?.GetType() == nuevoEstado.GetType()) return;
            estadoActual?.Salir(this, conocimiento, acciones);
            estadoActual = nuevoEstado;

            if (nuevoEstado is EstadoCerrarZona   ||
                nuevoEstado is EstadoYendoPalanca  ||
                nuevoEstado is EstadoPersecucion   ||
                nuevoEstado is EstadoBarrerMapa)
                conocimiento.TareaAsignada = TareaContrato.Ninguna;

            estadoActual.Entrar(this, conocimiento, acciones);
        }
    }
}