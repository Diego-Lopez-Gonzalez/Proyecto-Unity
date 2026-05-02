using System.Collections.Generic;
using UnityEngine;

namespace GuardiaIA
{
    public class BaseConocimiento
    {
        // Estado del jugador
        public Vector3 UltimaPosicionJugador    { get; set; }
        public bool    JugadorVisible           { get; set; }

        // Estado del objeto vigilado
        public bool ObjetoDesaparecido          { get; set; }
        public bool PalancaYaGestionada         { get; set; }
        public bool PalancaPendienteTrasPerder  { get; set; }

        // Datos de la palanca
        public Transform   Palanca              { get; set; }
        public Transform[] RutaTrasPalanca      { get; set; }

        // Datos de patrulla
        public Transform[] RutaPatrulla         { get; set; }
        public int         IndicePatrullaActual { get; set; }

        // Puntos de cierre del mapa (entradas, pasillos, zonas clave).
        // Se rellena desde Cerebro vía el Inspector con una List<Transform>.
        public List<Transform> PuntosCorte      { get; set; } = new List<Transform>();

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

        // Tarea asignada por Contract Net
        public TareaContrato TareaAsignada      { get; set; } = TareaContrato.Ninguna;
        public Vector3       PosicionCierreZona { get; set; }
        public int           IndiceZonaCorte    { get; set; }
        public string        ConversationIdTarea { get; set; }
    }
}