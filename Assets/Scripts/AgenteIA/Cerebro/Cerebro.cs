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

        // True cuando el jugador acaba de perderse (flag de un solo frame).
        public bool JugadorRecienPerdido        { get; set; }

        // Estado del objeto vigilado
        // ObjetoDesaparecido es un hecho permanente: nunca vuelve a false.
        public bool ObjetoDesaparecido          { get; set; }
        // PalancaYaGestionada evita que EvaluarPrioridades vuelva a mandar
        // al guardia a la palanca una vez que ya la activó (o lo intentó).
        // Se resetea a false cuando el estado de inspección decide revisarla de nuevo.
        public bool PalancaYaGestionada         { get; set; }

        // Si el objeto desaparece mientras perseguimos al jugador,
        // guardamos que hay una palanca pendiente para ir después de perderle.
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
        public Vector3 PosicionPercibidaSonido  { get; set; }
        public float   RadioIncertidumbreSonido { get; set; }
        public bool    SonidoPendiente          { get; set; }
    }


    //
    //  CEREBRO
    //  Orquesta sensores, estado activo y acciones.
    //  Es el único punto que cambia de estado y actualiza el conocimiento.
    //
    public class Cerebro : MonoBehaviour
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

            acciones     = GetComponent<Acciones>();
            sensorVision = GetComponent<SensorVision>();
            sensorTacto  = GetComponent<SensorTacto>();
            sensorSonido = GetComponent<SensorSonido>();

            sensorVision.Inicializar(this, jugador, objetoVigilado,
                                     rangoVision, anguloVision, capasObstaculo);
            sensorTacto.Inicializar(this, jugador, radioCaptura, rangoDeteccionPuertas);
            sensorSonido.Inicializar(this, radioEscucha, radioIncertidumbre);

            CambiarEstado(new EstadoPatrulla());
        }

        private void Update()
        {
            // 1. El evaluador centralizado decide qué estado debe estar activo.
            EvaluarPrioridades();

            // 2. El estado activo ejecuta su lógica interna.
            estadoActual?.Ejecutar(this, conocimiento, acciones);
        }

        //
        //  EVALUADOR DE PRIORIDADES
        //
        private void EvaluarPrioridades()
        {
            // Prioridad 1: jugador visible → perseguir siempre
            if (conocimiento.JugadorVisible)
            {
                if (estadoActual is not EstadoPersecucion)
                {
                    // Si interrumpimos la palanca a mitad, la recordamos para después
                    if (estadoActual is EstadoYendoPalanca)
                        conocimiento.PalancaPendienteTrasPerder = true;

                    CambiarEstado(new EstadoPersecucion());
                }
                return;
            }

            // Prioridad 2: jugador recién perdido
            if (conocimiento.JugadorRecienPerdido)
            {
                conocimiento.JugadorRecienPerdido = false;

                if (conocimiento.PalancaPendienteTrasPerder)
                {
                    conocimiento.PalancaPendienteTrasPerder = false;
                    CambiarEstado(new EstadoYendoPalanca());
                }
                else
                {
                    CambiarEstado(new EstadoBusqueda());
                }
                return;
            }

            // Prioridad 3: sonido pendiente
            // Solo interrumpimos patrulla o inspección, no búsqueda activa.
            if (conocimiento.SonidoPendiente)
            {
                conocimiento.SonidoPendiente = false;

                if (estadoActual is EstadoPatrulla or EstadoInspeccionAleatoria)
                    CambiarEstado(new EstadoInvestigarSonido());

                return;
            }

            // Prioridad 4: objeto robado, palanca aún no gestionada
            // Primera vez que el objeto desaparece: ir a activar la palanca.
            if (conocimiento.ObjetoDesaparecido    &&
                !conocimiento.PalancaYaGestionada  &&
                estadoActual is not EstadoYendoPalanca &&
                estadoActual is not EstadoPersecucion)
            {
                CambiarEstado(new EstadoYendoPalanca());
                return;
            }

            // Prioridad 5: objeto robado, palanca ya gestionada
            // El objeto sigue sin estar: en vez de patrullar normalmente,
            // el guardia hace inspecciones más exhaustivas por la zona.
            // Si el estado actual es Patrulla normal, lo redirigimos.
            if (conocimiento.ObjetoDesaparecido   &&
                conocimiento.PalancaYaGestionada  &&
                estadoActual is EstadoPatrulla)
            {
                CambiarEstado(new EstadoInspeccionAleatoria());
                return;
            }

            // Prioridad 6: nada urgente
            // llaman a CambiarEstado(new EstadoPatrulla()) cuando terminan,
            // y la prioridad 5 se encarga de redirigir si hace falta.
        }

        //
        //  CALLBACKS DE SENSORES
        //  Solo escriben en BaseConocimiento; EvaluarPrioridades decide qué hacer.
        //

        public void OnJugadorDetectado(Vector3 posicion)
        {
            conocimiento.JugadorVisible        = true;
            conocimiento.UltimaPosicionJugador = posicion;
        }

        public void OnActualizarPosicionJugador(Vector3 posicion)
        {
            conocimiento.UltimaPosicionJugador = posicion;
        }

        public void OnJugadorPerdido()
        {
            conocimiento.JugadorVisible       = false;
            conocimiento.JugadorRecienPerdido = true;
        }

        public void OnObjetoDesaparecido()
        {
            // Hecho permanente: el objeto ya no está, no se resetea nunca.
            conocimiento.ObjetoDesaparecido = true;
        }

        public void OnSonidoDetectado(Vector3 posicionPercibida, float radioDesvio)
        {
            conocimiento.PosicionPercibidaSonido  = posicionPercibida;
            conocimiento.RadioIncertidumbreSonido = radioDesvio;
            conocimiento.SonidoPendiente          = true;
        }

        // Llamado por EstadoYendoPalanca al terminar (palanca activada o abortada).
        // Marca que la misión de la palanca ya está resuelta.
        public void OnPalancaGestionada()
        {
            conocimiento.PalancaYaGestionada = true;
        }

        // Llamado por EstadoInspeccionAleatoria cuando decide revisar la palanca
        // de nuevo tras pasar demasiado tiempo sin novedad.

        public void OnRevisarPalanca()
        {
            conocimiento.PalancaYaGestionada = false;
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
            estadoActual?.Salir(this, conocimiento, acciones);
            estadoActual = nuevoEstado;
            estadoActual.Entrar(this, conocimiento, acciones);
        }
    }
}