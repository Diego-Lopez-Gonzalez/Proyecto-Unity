using System.Collections.Generic;
using UnityEngine;

namespace GuardiaIA
{
    /// Implementa el protocolo Contract Net de FIPA sobre el buzón de mensajes.
    ///
    /// Cada agente puede actuar como GESTOR o CONTRATISTA en distintos momentos:
    ///
    ///   GESTOR  — cuando detecta al ladrón, lanza un Cfp al resto, recoge
    ///             propuestas y asigna tareas (Perseguir / CerrarZona).
    ///
    ///   CONTRATISTA — cuando recibe un Cfp, evalúa si puede ayudar y responde
    ///                 con Propose o Refuse. Si es aceptado, ejecuta la tarea.
    ///
    /// El gestor no es un rol fijo: cualquier agente que vea al ladrón primero
    /// se convierte en gestor para esa conversación.
    /// Las cámaras nunca son contratistas: EstaEnPersecucion = true las excluye.
    public class GestorComunicacion : MonoBehaviour
    {
        // ── Vecinos ──────────────────────────────────────────────────────────
        // Cada agente descubre a sus vecinos en Start sin ningún registro central.
        private List<GestorComunicacion> vecinos = new List<GestorComunicacion>();

        // ── Componentes propios ───────────────────────────────────────────────
        private BuzonMensajes buzon;
        private IAgente       agente;                     // antes: Cerebro cerebro

        // Acceso interno al buzón propio: necesario para que Enviar() deposite
        // mensajes en el buzón del receptor sin usar GetComponent cada vez.
        internal BuzonMensajes Buzon => buzon;

        // ── Estado del protocolo como GESTOR ─────────────────────────────────
        private bool   esperandoPropuestas    = false;
        private float  timerEsperaPropuestas  = 0f;
        private const float TIMEOUT_PROPUESTAS = 0.5f; // segundos esperando offers

        private string                     conversacionActual;
        private Vector3                    posicionLadronCfp;
        private List<MensajeACL>           propuestasRecibidas = new List<MensajeACL>();

        // ── Estado del protocolo como CONTRATISTA ────────────────────────────
        // conversationId de la conversación en la que participamos como contratista
        private string conversacionComoContratista;


        private void Awake()
        {
            buzon  = GetComponent<BuzonMensajes>();
            // GetComponent devuelve el primer componente que implemente IAgente
            // (Cerebro o CerebroCamara) sin importar el tipo concreto.
            agente = GetComponent<IAgente>();
        }

        private void Start()
        {
            // Descubrimiento de vecinos: buscamos todos los gestores en escena
            // excepto nosotros mismos. Sin singleton, sin registro central.
            foreach (var g in FindObjectsOfType<GestorComunicacion>())
            {
                if (g != this)
                    vecinos.Add(g);
            }

            Debug.Log($"[GestorComunicacion] {name} conoce {vecinos.Count} vecinos.");
        }

        private void Update()
        {
            ProcesarBuzon();

            // Si somos gestores esperando propuestas, comprobamos el timeout
            if (esperandoPropuestas)
            {
                timerEsperaPropuestas -= Time.deltaTime;
                if (timerEsperaPropuestas <= 0f)
                    AdjudicarTareas();
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  API PÚBLICA — llamada desde IAgente / estados
        // ════════════════════════════════════════════════════════════════════

        /// El agente acaba de ver al ladrón y lanza el Contract Net como gestor.
        /// Solo inicia una conversación si no hay una ya activa.
        public void IniciarContractNet(Vector3 posicionLadron)
        {
            if (esperandoPropuestas) return;

            conversacionActual   = $"cn_{name}_{Time.frameCount}";
            posicionLadronCfp    = posicionLadron;
            propuestasRecibidas.Clear();
            esperandoPropuestas  = true;
            timerEsperaPropuestas = TIMEOUT_PROPUESTAS;

            Debug.Log($"[GestorComunicacion] {name} inicia Contract Net conv:{conversacionActual}");

            // ANUNCIO: Cfp en broadcast a todos los vecinos
            foreach (var vecino in vecinos)
            {
                var cfp = new MensajeACL
                {
                    Performativa   = Performativa.Cfp,
                    Emisor         = this,                // MonoBehaviour, no IAgente
                    Receptor       = vecino,              // ídem
                    ConversationId = conversacionActual,
                    Contenido      = new ContenidoMensaje { PosicionLadron = posicionLadron }
                };
                vecino.buzon.Recibir(cfp);
            }
        }

        /// Notifica al gestor de una conversación que la tarea fue completada.
        public void NotificarTareaCompletada(string conversationId)
        {
            foreach (var vecino in vecinos)
            {
                var msg = new MensajeACL
                {
                    Performativa   = Performativa.InformDone,
                    Emisor         = this,
                    Receptor       = vecino,
                    ConversationId = conversationId,
                    InReplyTo      = conversationId
                };
                vecino.buzon.Recibir(msg);
            }
        }

        // ════════════════════════════════════════════════════════════════════
        //  PROCESADO DEL BUZÓN
        // ════════════════════════════════════════════════════════════════════

        private void ProcesarBuzon()
        {
            foreach (var msg in buzon.ProcesarPendientes())
            {
                switch (msg.Performativa)
                {
                    case Performativa.Cfp:
                        ProcesarCfp(msg);
                        break;

                    case Performativa.Propose:
                        ProcesarPropose(msg);
                        break;

                    case Performativa.AcceptProposal:
                        ProcesarAcceptProposal(msg);
                        break;

                    case Performativa.RejectProposal:
                        // Nada que hacer: simplemente no participamos.
                        Debug.Log($"[GestorComunicacion] {name} propuesta rechazada en {msg.ConversationId}");
                        break;

                    case Performativa.InformDone:
                        Debug.Log($"[GestorComunicacion] {name} recibió InformDone de {msg.Emisor?.name}");
                        break;

                    case Performativa.Inform:
                        ProcesarInform(msg);
                        break;
                }
            }
        }

        // ── Rol CONTRATISTA ──────────────────────────────────────────────────

        private void ProcesarCfp(MensajeACL cfp)
        {
            // Si ya estamos en una conversación como contratista, rechazamos.
            // Si EstaEnPersecucion = true (guardias persiguiendo o cámaras siempre),
            // también rechazamos: las cámaras quedan automáticamente excluidas.
            bool ocupado = conversacionComoContratista != null
                        || agente.EstaEnPersecucion;

            if (ocupado)
            {
                Enviar(new MensajeACL
                {
                    Performativa   = Performativa.Refuse,
                    Emisor         = this,
                    Receptor       = cfp.Emisor,
                    ConversationId = cfp.ConversationId,
                    InReplyTo      = cfp.ConversationId
                });
                return;
            }

            // Podemos ayudar: enviamos Propose con nuestra distancia al ladrón
            // para que el gestor pueda asignar tareas por proximidad.
            float distancia = Vector3.Distance(transform.position, cfp.Contenido.PosicionLadron);

            conversacionComoContratista = cfp.ConversationId;

            Enviar(new MensajeACL
            {
                Performativa   = Performativa.Propose,
                Emisor         = this,
                Receptor       = cfp.Emisor,
                ConversationId = cfp.ConversationId,
                InReplyTo      = cfp.ConversationId,
                Contenido      = new ContenidoMensaje
                {
                    PosicionLadron    = cfp.Contenido.PosicionLadron,
                    DistanciaAlLadron = distancia
                }
            });

            Debug.Log($"[GestorComunicacion] {name} propone para {cfp.ConversationId} (dist:{distancia:F1})");
        }

        private void ProcesarAcceptProposal(MensajeACL msg)
        {
            Debug.Log($"[GestorComunicacion] {name} aceptado → tarea: {msg.Contenido.Tarea}");

            switch (msg.Contenido.Tarea)
            {
                case TareaContrato.CerrarZona:
                    agente.OnAsignadoCerrarZona(msg.Contenido.PosicionLadron, msg.ConversationId);
                    break;

                case TareaContrato.IrAPalanca:
                    agente.OnAsignadoIrAPalanca(msg.ConversationId);
                    break;
            }
        }

        private void ProcesarInform(MensajeACL msg)
        {
            // Un vecino nos informa de la posición del ladrón sin iniciar contrato.
            // Lo tratamos igual que si nuestro sensor lo hubiera visto.
            agente.OnJugadorDetectado(msg.Contenido.PosicionLadron);
        }

        // ── Rol GESTOR ───────────────────────────────────────────────────────

        private void ProcesarPropose(MensajeACL prop)
        {
            // Solo aceptamos propuestas de nuestra conversación activa
            if (prop.ConversationId != conversacionActual) return;
            propuestasRecibidas.Add(prop);
        }

        /// Se llama cuando expira el timer de espera de propuestas.
        /// El gestor ya está persiguiendo por cuenta propia.
        /// Asigna tareas a los contratistas según proximidad al ladrón:
        ///   · El más cercano cierra la zona de escape (CerrarZona).
        ///   · El segundo activa la palanca de alarma (IrAPalanca).
        ///   · El tercero y siguientes reciben RejectProposal y no cambian de estado.
        private void AdjudicarTareas()
        {
            esperandoPropuestas = false;

            // Ordenamos por distancia al ladrón (el más cercano primero)
            propuestasRecibidas.Sort((a, b) =>
                a.Contenido.DistanciaAlLadron.CompareTo(b.Contenido.DistanciaAlLadron));

            Debug.Log($"[GestorComunicacion] {name} adjudicando con {propuestasRecibidas.Count} propuestas.");

            for (int i = 0; i < propuestasRecibidas.Count; i++)
            {
                var prop = propuestasRecibidas[i];

                if (i == 0)
                {
                    // El más cercano cierra la zona de escape
                    Enviar(new MensajeACL
                    {
                        Performativa   = Performativa.AcceptProposal,
                        Emisor         = this,
                        Receptor       = prop.Emisor,
                        ConversationId = conversacionActual,
                        InReplyTo      = prop.ConversationId,
                        Contenido      = new ContenidoMensaje
                        {
                            Tarea          = TareaContrato.CerrarZona,
                            PosicionLadron = posicionLadronCfp
                        }
                    });
                }
                else if (i == 1)
                {
                    // El segundo va a activar la palanca
                    Enviar(new MensajeACL
                    {
                        Performativa   = Performativa.AcceptProposal,
                        Emisor         = this,
                        Receptor       = prop.Emisor,
                        ConversationId = conversacionActual,
                        InReplyTo      = prop.ConversationId,
                        Contenido      = new ContenidoMensaje
                        {
                            Tarea          = TareaContrato.IrAPalanca,
                            PosicionLadron = posicionLadronCfp
                        }
                    });
                }
                else
                {
                    // El resto recibe rechazo: no cambia de estado, solo queda libre
                    Enviar(new MensajeACL
                    {
                        Performativa   = Performativa.RejectProposal,
                        Emisor         = this,
                        Receptor       = prop.Emisor,
                        ConversationId = conversacionActual,
                        InReplyTo      = prop.ConversationId
                    });

                    prop.Emisor?.LiberarConversacion(conversacionActual);
                }
            }

            propuestasRecibidas.Clear();
            conversacionActual = null;
        }

        // ════════════════════════════════════════════════════════════════════
        //  UTILIDADES
        // ════════════════════════════════════════════════════════════════════

        /// Libera la conversación de contratista (llamado por el gestor
        /// al rechazar o al terminar la conversación).
        public void LiberarConversacion(string conversationId)
        {
            if (conversacionComoContratista == conversationId)
                conversacionComoContratista = null;
        }

        private void Enviar(MensajeACL mensaje)
        {
            Debug.Log($"[GestorComunicacion] Enviando: {mensaje}");
            mensaje.Receptor?.Buzon.Recibir(mensaje);
        }
    }
}