using UnityEngine;

namespace GuardiaIA
{
    //
    //  CEREBRO
    //  Orquesta sensores, estado activo y acciones.
    //  Es el único punto que cambia de estado y evalúa prioridades.
    //
    //  Implementa IAgente para que SensorVision y GestorComunicacion no dependan
    //  del tipo concreto: cualquier agente que implemente IAgente puede usar
    //  esos componentes sin modificación.
    //
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

        // IAgente: expone si estamos en persecución activa para que
        // GestorComunicacion pueda decidir si este agente está disponible
        // como contratista.
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
                RadioBusqueda        = radioBusqueda
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
            // El estado activo ejecuta su lógica interna.
            estadoActual?.Ejecutar(this, conocimiento, acciones);

            // Si el estado señala que ha terminado su tarea, el árbitro
            // decide qué estado corresponde ahora según las prioridades.
            if (estadoActual != null && estadoActual.HaTerminado)
                EvaluarPrioridades();
        }

        //
        //  EVALUADOR DE PRIORIDADES  (subsunción)
        //
        //  Solo se invoca desde dos sitios:
        //    · Callbacks de sensores  → estímulo externo nuevo
        //    · Update                 → el estado activo ha terminado su tarea
        //
        //  Lee el conocimiento de arriba abajo y aplica la primera regla
        //  cuya condición es verdadera. Si ninguna encaja, vuelve al estado base.
        //
        private void EvaluarPrioridades()
        {
            // Prioridad 1: jugador visible → perseguir siempre.
            if (conocimiento.JugadorVisible)
            {
                if (estadoActual is EstadoYendoPalanca)
                    conocimiento.PalancaPendienteTrasPerder = true;

                // Si nos interrumpen una tarea asignada, la cancelamos.
                conocimiento.TareaAsignada = TareaContrato.Ninguna;

                CambiarEstado(new EstadoPersecucion());
                return;
            }

            // Prioridad 2: sonido pendiente → investigar.
            if (conocimiento.SonidoPendiente)
            {
                conocimiento.SonidoPendiente = false;
                CambiarEstado(new EstadoInvestigarSonido());
                return;
            }

            // Prioridad 3: objeto robado, palanca aún no gestionada → ir a palanca.
            // Tiene prioridad sobre las tareas asignadas por Contract Net.
            if (conocimiento.ObjetoDesaparecido && !conocimiento.PalancaYaGestionada)
            {
                CambiarEstado(new EstadoYendoPalanca());
                return;
            }

            // Prioridad 4: tarea asignada por Contract Net → cerrar zona.
            if (conocimiento.TareaAsignada == TareaContrato.CerrarZona)
            {
                CambiarEstado(new EstadoCerrarZona(conocimiento.ConversationIdTarea));
                return;
            }

            // Prioridad 5: tarea asignada por Contract Net → ir a palanca.
            if (conocimiento.TareaAsignada == TareaContrato.IrAPalanca)
            {
                CambiarEstado(new EstadoYendoPalanca(conocimiento.ConversationIdTarea));
                return;
            }

            // Prioridad 6: objeto robado, palanca ya gestionada → inspección profunda.
            if (conocimiento.ObjetoDesaparecido && conocimiento.PalancaYaGestionada)
            {
                CambiarEstado(new EstadoInspeccionAleatoria());
                return;
            }

            // Prioridad 7 (base): patrullar.
            CambiarEstado(new EstadoPatrulla());
        }

        //
        //  CALLBACKS DE SENSORES  (IAgente + propios)
        //  Escriben en BaseConocimiento y disparan EvaluarPrioridades
        //  si el estímulo puede provocar un cambio de estado.
        //

        public void OnJugadorDetectado(Vector3 posicion)
        {
            conocimiento.JugadorVisible        = true;
            conocimiento.UltimaPosicionJugador = posicion;

            // Iniciamos el Contract Net solo si somos nosotros quien lo detecta
            // por primera vez (no si nos lo comunicó otro agente, para evitar
            // que se lancen contratos en cascada).
            gestorComunicacion?.IniciarContractNet(posicion);

            EvaluarPrioridades();
        }

        public void OnActualizarPosicionJugador(Vector3 posicion)
        {
            // Solo actualiza el dato: el estado ya es Persecución, no hace falta reevaluar.
            conocimiento.UltimaPosicionJugador = posicion;
        }

        public void OnJugadorPerdido()
        {
            conocimiento.JugadorVisible = false;

            // Decidimos el sucesor aquí mismo, donde tenemos todo el contexto,
            // en lugar de usar un flag de "recién perdido" que se procesaría
            // un frame más tarde.
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
            // Hecho permanente: el objeto ya no está, no se resetea nunca.
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

        // Llamado por EstadoYendoPalanca al terminar (desde Salir).
        public void OnPalancaGestionada()
        {
            conocimiento.PalancaYaGestionada = true;
        }

        // Llamado por EstadoInspeccionAleatoria cuando decide revisar la palanca
        // de nuevo. Escribe el hecho en el conocimiento; EvaluarPrioridades
        // (invocada por HaTerminado en Update) lo leerá y activará YendoPalanca.
        public void OnRevisarPalanca()
        {
            conocimiento.PalancaYaGestionada = false;
        }

        // ── Callbacks del Contract Net (IAgente) ─────────────────────────────

        /// El gestor nos asignó la tarea de cerrar la zona de escape.
        /// Usamos TareaAsignada para que EvaluarPrioridades active el estado correcto.
        public void OnAsignadoCerrarZona(Vector3 posicionLadron, string conversationId)
        {
            Debug.Log($"[Cerebro] {name} asignado a CERRAR ZONA conv:{conversationId}");
            conocimiento.UltimaPosicionJugador  = posicionLadron;
            conocimiento.TareaAsignada          = TareaContrato.CerrarZona;
            conocimiento.ConversationIdTarea    = conversationId;
            EvaluarPrioridades();
        }

        /// El gestor nos asignó la tarea de activar la palanca de alarma.
        /// Usamos TareaAsignada para que EvaluarPrioridades active el estado correcto.
        public void OnAsignadoIrAPalanca(string conversationId)
        {
            Debug.Log($"[Cerebro] {name} asignado a IR A PALANCA conv:{conversationId}");
            conocimiento.TareaAsignada       = TareaContrato.IrAPalanca;
            conocimiento.ConversationIdTarea = conversationId;
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

        //
        //  GESTIÓN DE ESTADOS
        //

        public void CambiarEstado(IEstado nuevoEstado)
        {
            if (estadoActual?.GetType() == nuevoEstado.GetType()) return;
            estadoActual?.Salir(this, conocimiento, acciones);
            estadoActual = nuevoEstado;

            // Consumimos la tarea asignada al activar el estado que la cumple,
            // para que EvaluarPrioridades no la vuelva a disparar cuando termine.
            if (nuevoEstado is EstadoCerrarZona || nuevoEstado is EstadoYendoPalanca)
                conocimiento.TareaAsignada = TareaContrato.Ninguna;

            estadoActual.Entrar(this, conocimiento, acciones);
        }
    }
}