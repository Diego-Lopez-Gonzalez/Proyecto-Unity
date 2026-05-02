using System.Collections.Generic;
using UnityEngine;

namespace GuardiaIA
{
    public class BaseConocimiento
    {
        public Vector3 UltimaPosicionJugador    { get; set; }
        public bool    JugadorVisible           { get; set; }

        public bool ObjetoDesaparecido          { get; set; }
        public bool PalancaYaGestionada         { get; set; }
        public bool PalancaPendienteTrasPerder  { get; set; }

        public Transform   Palanca              { get; set; }
        public Transform[] RutaTrasPalanca      { get; set; }

        public Transform[] RutaPatrulla         { get; set; }
        public int         IndicePatrullaActual { get; set; }

        public List<Transform> PuntosCorte      { get; set; } = new List<Transform>();

        public float VelocidadPatrulla          { get; set; }
        public float VelocidadPersecucion       { get; set; }

        public float TiempoEsperaEnPunto        { get; set; }
        public float TiempoBusqueda             { get; set; }
        public float RadioBusqueda              { get; set; }

        public Vector3 PosicionPercibidaSonido  { get; set; }
        public float   RadioIncertidumbreSonido { get; set; }
        public bool    SonidoPendiente          { get; set; }

        public TareaContrato TareaAsignada       { get; set; } = TareaContrato.Ninguna;
        public Vector3       PosicionCierreZona  { get; set; }
        public int           IndiceZonaCorte     { get; set; }
        public string        ConversationIdTarea  { get; set; }

        /// Prioridad del contrato activo. Se actualiza junto con TareaAsignada.
        /// Se usa en IAgente.PrioridadTareaActual para comparar con contratos entrantes.
        public PrioridadContrato PrioridadTareaActual { get; set; } = PrioridadContrato.Baja;
    }
}