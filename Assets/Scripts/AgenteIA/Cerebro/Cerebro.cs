using UnityEngine;

namespace GuardiaIA
{
    //
    //  BASE DE CONOCIMIENTO
    //  Solo datos: sin lógica ni métodos propios.
    //
    public class BaseConocimiento
    {
        // Estado del jugador
        public Vector3 UltimaPosicionJugador    { get; set; }
        public bool    JugadorVisible           { get; set; }

        // Estado del objeto vigilado
        public bool ObjetoDesaparecido          { get; set; }

        // Gestión de prioridades entre persecución y palanca
        // True cuando el objeto desaparece mientras se persigue al jugador.
        // Al perder al jugador el cerebro sabe que debe ir a la palanca.
        public bool PalancaPendienteTrasPerder  { get; set; }

        // Datos de la palanca
        public Transform   Palanca              { get; set; }
        public Transform[] RutaTrasPalanca      { get; set; }

        // Datos de patrulla
        public Transform[] RutaPatrulla         { get; set; }
        public int         IndicePatrullaActual { get; set; }

        // Parámetros de movimiento
        public float VelocidadPatrulla          { get; set; }
        public float VelocidadPersecucion       { get; set; }

        // Parámetros de comportamiento
        public float TiempoEsperaEnPunto        { get; set; }
        public float TiempoBusqueda             { get; set; }
        public float RadioBusqueda              { get; set; }

        // Datos de sonido
        // Posición aproximada donde el agente creyó escuchar el sonido.
        public Vector3 PosicionPercibidaSonido  { get; set; }
        // Radio de incertidumbre del sonido percibido.
        public float   RadioIncertidumbreSonido { get; set; }
    }


    //
    //  CEREBRO
    //  Orquesta sensores, estado activo y acciones.
    //  Es el único punto que cambia de estado y actualiza el conocimiento.
    //
    public class Cerebro : MonoBehaviour
    {
        // ── Inspector ───────────────────────────────────────────────────
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
        private BaseConocimiento conocimiento;
        private IEstado          estadoActual;
        private Acciones         acciones;
        private SensorVision     sensorVision;
        private SensorTacto      sensorTacto;
        private SensorSonido     sensorSonido;

        // Acceso para los estados
        public SensorTacto SensorTacto => sensorTacto;


        private void Start()
        {
            // 1. Construir la base de conocimiento con todos los parámetros
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

            // 2. Obtener componentes del mismo GameObject
            acciones     = GetComponent<Acciones>();
            sensorVision = GetComponent<SensorVision>();
            sensorTacto  = GetComponent<SensorTacto>();

            // 3. Inicializar sensores: les damos nuestra referencia.
            //    A partir de aquí los sensores nos avisarán y nosotros nunca les preguntamos.
            sensorVision.Inicializar(
                this,
                jugador,
                objetoVigilado,
                rangoVision,
                anguloVision,
                capasObstaculo
            );

            sensorTacto.Inicializar(
                this,
                jugador,
                radioCaptura,
                rangoDeteccionPuertas
            );

            sensorSonido = GetComponent<SensorSonido>();
            sensorSonido.Inicializar(this, radioEscucha, radioIncertidumbre);

            // 4. Estado inicial
            CambiarEstado(new EstadoPatrulla());
        }

        private void Update()
        {
            // El cerebro solo delega en el estado activo.
            estadoActual?.Ejecutar(this, conocimiento, acciones);
        }

        //
        //  MÉTODOS PÚBLICOS — llamados exclusivamente por sensores
        //

        // El sensor de visión ha detectado al jugador por primera vez o tras perderlo.
        public void OnJugadorDetectado(Vector3 posicion)
        {
            conocimiento.JugadorVisible           = true;
            conocimiento.UltimaPosicionJugador    = posicion;

            // Si ya estábamos persiguiendo no cambiamos estado
            if (estadoActual is EstadoPersecucion) return;

            // Si interrumpimos la secuencia de palanca, la recordamos para después
            if (estadoActual is EstadoYendoPalanca)
                conocimiento.PalancaPendienteTrasPerder = true;

            CambiarEstado(new EstadoPersecucion());
        }

        // El jugador sigue visible y actualizamos su posición
        // en el conocimiento sin cambiar de estado.
        public void OnActualizarPosicionJugador(Vector3 posicion)
        {
            conocimiento.UltimaPosicionJugador = posicion;
        }

        // El sensor de visión ha perdido al jugador.
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

        // El sensor ha detectado que el objeto vigilado ha desaparecido.
        public void OnObjetoDesaparecido()
        {
            conocimiento.ObjetoDesaparecido = true;

            if (estadoActual is EstadoPersecucion)
            {
                // No interrumpimos la persecución, lo recordamos para después
                conocimiento.PalancaPendienteTrasPerder = true;
            }
            else if (!(estadoActual is EstadoYendoPalanca))
            {
                CambiarEstado(new EstadoYendoPalanca());
            }
            // Si ya estamos yendo a la palanca no hacemos nada
        }

        // El sensor de sonido ha detectado ruido en un área aproximada.
        public void OnSonidoDetectado(Vector3 posicionPercibida, float radioDesvio)
        {
            // El sonido tiene menor prioridad que la visión directa:
            // ignoramos si ya estamos persiguiendo, capturando o yendo a la palanca
            if (estadoActual is EstadoPersecucion)  return;
            if (estadoActual is EstadoYendoPalanca) return;

            // Guardamos el área percibida en el conocimiento
            conocimiento.PosicionPercibidaSonido  = posicionPercibida;
            conocimiento.RadioIncertidumbreSonido = radioDesvio;

            // Solo investigamos si no estábamos ya investigando este mismo sonido
            if (estadoActual is EstadoInvestigarSonido) return;

            Debug.Log($"[Cerebro] Sonido detectado → INVESTIGAR área {posicionPercibida}");
            CambiarEstado(new EstadoInvestigarSonido());
        }

        // El sensor de captura ha detectado que el jugador está
        // dentro del radio de captura.
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
            estadoActual?.Salir(this, conocimiento, acciones);
            estadoActual = nuevoEstado;
            estadoActual.Entrar(this, conocimiento, acciones);
        }
    }
}