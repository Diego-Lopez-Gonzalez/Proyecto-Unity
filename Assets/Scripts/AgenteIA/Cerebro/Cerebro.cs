using System.Collections.Generic;
using UnityEngine;

namespace GuardiaIA
{
    public class Cerebro : MonoBehaviour, IAgente
    {
        // Inspector
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

        // Componentes internos
        private BaseConocimiento   conocimiento;
        private IEstado            estadoActual;
        private Acciones           acciones;
        private SensorVision       sensorVision;
        private SensorTacto        sensorTacto;
        private SensorSonido       sensorSonido;
        private GestorComunicacion gestorComunicacion;

        // Acceso para los estados y el gestor
        public SensorTacto         SensorTacto        => sensorTacto;
        public GestorComunicacion  GestorComunicacion => gestorComunicacion;

        public bool EstaEnPersecucion => estadoActual is EstadoPersecucion;


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
                PuntosCorte          = puntosCorte
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

        private void EvaluarPrioridades()
        {
            if (conocimiento.JugadorVisible)
            {
                if (estadoActual is EstadoYendoPalanca)
                    conocimiento.PalancaPendienteTrasPerder = true;

                conocimiento.TareaAsignada = TareaContrato.Ninguna;

                CambiarEstado(new EstadoPersecucion());
                return;
            }

            if (conocimiento.SonidoPendiente)
            {
                conocimiento.SonidoPendiente = false;
                CambiarEstado(new EstadoInvestigarSonido());
                return;
            }

            if (conocimiento.ObjetoDesaparecido && !conocimiento.PalancaYaGestionada)
            {
                CambiarEstado(new EstadoYendoPalanca());
                return;
            }

            if (conocimiento.TareaAsignada == TareaContrato.CerrarZona)
            {
                CambiarEstado(new EstadoCerrarZona(conocimiento.ConversationIdTarea));
                return;
            }

            if (conocimiento.TareaAsignada == TareaContrato.IrAPalanca)
            {
                CambiarEstado(new EstadoYendoPalanca(conocimiento.ConversationIdTarea));
                return;
            }

            if (conocimiento.ObjetoDesaparecido && conocimiento.PalancaYaGestionada)
            {
                CambiarEstado(new EstadoInspeccionAleatoria());
                return;
            }

            CambiarEstado(new EstadoPatrulla());
        }

        public void OnJugadorDetectado(Vector3 posicion)
        {
            conocimiento.JugadorVisible        = true;
            conocimiento.UltimaPosicionJugador = posicion;

            gestorComunicacion?.IniciarContractNet(
                posicion,
                new[] { TareaContrato.CerrarZona, TareaContrato.IrAPalanca }
            );

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

        public void OnTareaAsignada(TareaContrato tarea, string conversationId)
        {
            Debug.Log($"[Cerebro] {name} tarea asignada: {tarea} conv:{conversationId}");
            conocimiento.TareaAsignada       = tarea;
            conocimiento.ConversationIdTarea = conversationId;
            EvaluarPrioridades();
        }

        public void OnTareaCancelada(string conversationId)
        {
            Debug.Log($"[Cerebro] {name} tarea cancelada conv:{conversationId}");
            conocimiento.TareaAsignada       = TareaContrato.Ninguna;
            conocimiento.ConversationIdTarea = null;
            EvaluarPrioridades();
        }

        public void OnJugadorCapturado()
        {
            Debug.Log("[Cerebro] ¡JUGADOR ATRAPADO! → GAME OVER");
            GameOverManager.Instancia.ActivarGameOver();
        }

        public void OnPuertaDetectada(PuertaNavMesh puerta)
        {
            puerta.Abrir();
        }

        public void CambiarEstado(IEstado nuevoEstado)
        {
            if (estadoActual?.GetType() == nuevoEstado.GetType()) return;
            estadoActual?.Salir(this, conocimiento, acciones);
            estadoActual = nuevoEstado;

            if (nuevoEstado is EstadoCerrarZona || nuevoEstado is EstadoYendoPalanca)
                conocimiento.TareaAsignada = TareaContrato.Ninguna;

            estadoActual.Entrar(this, conocimiento, acciones);
        }
    }
}