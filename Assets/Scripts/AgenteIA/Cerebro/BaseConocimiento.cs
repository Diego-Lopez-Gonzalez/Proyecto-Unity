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
        // ObjetoDesaparecido es un hecho permanente: nunca vuelve a false.
        public bool ObjetoDesaparecido          { get; set; }

        // PalancaYaGestionada evita que EvaluarPrioridades vuelva a mandar
        // al guardia a la palanca una vez que ya la activó (o lo intentó).
        // Se resetea a false cuando EstadoInspeccionAleatoria decide revisarla de nuevo.
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

        // Tarea asignada por Contract Net.
        // El árbitro la lee como cualquier otro hecho y la consume al activar el estado.
        // Se resetea a Ninguna cuando el estado termina o es subsumido.
        public TareaContrato TareaAsignada      { get; set; } = TareaContrato.Ninguna;
        public Vector3       PosicionCierreZona { get; set; }
        public int           IndiceZonaCorte    { get; set; } // qué punto de patrulla cubrir
        public string        ConversationIdTarea { get; set; }
    }
}