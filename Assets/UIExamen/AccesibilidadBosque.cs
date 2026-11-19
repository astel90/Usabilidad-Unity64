using System.Linq;
using StarterAssets;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

[RequireComponent(typeof(Canvas), typeof(CanvasScaler))]
public sealed class AccesibilidadBosque : MonoBehaviour
{
    [SerializeField] private RectTransform indicadorEstrellas;

    private GameManager juego;
    private TrapController[] trampas;
    private StarterAssetsInputs personaje;
    private Image[] estrellas;
    [SerializeField] private Sprite estrellaLlena, estrellaVacia;
    private Font fuente;
    private Text progreso, objetivo, aviso, subtitulo, textoDaltonismo;
    private RectTransform cajaSubtitulo, opciones;
    private Button primerBoton;
    private bool menuAbierto, daltonismo;
    private bool cursorBloqueado, controlarMirada, cursorVisible;
    private CursorLockMode bloqueoAnterior;
    private float tiempoAnterior, avisoHasta;
    private AudioSource audioAviso;
    private AudioClip tono;
    private bool cercaDeTrampa;
    private string mensaje;

    private void Start()
    {
        juego = GameManager.Instance;
        if (juego == null || indicadorEstrellas == null)
        {
            Debug.LogError("Faltan GameManager o el indicador de estrellas.", this);
            enabled = false;
            return;
        }

        if (EventSystem.current == null && FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        personaje = FindFirstObjectByType<ThirdPersonController>()?.GetComponent<StarterAssetsInputs>();
        trampas = FindObjectsByType<TrapController>(FindObjectsSortMode.None);
        fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        estrellas = indicadorEstrellas.GetComponentsInChildren<Image>()
            .Where(icono => icono.transform.parent == indicadorEstrellas)
            .OrderBy(icono => icono.rectTransform.anchoredPosition.x).ToArray();
        for (int i = 0; i < estrellas.Length; i++)
            estrellas[i].gameObject.SetActive(i < Mathf.Max(1, juego.starsRequired));

        progreso = Etiqueta(Caja("Progreso", transform, new Vector2(0, 1),
            new Vector2(208, -42), new Vector2(125, 48)), "", TextAnchor.MiddleCenter);
        objetivo = Etiqueta(Caja("Objetivo", transform, new Vector2(0, 1),
            new Vector2(251, -107), new Vector2(466, 68)), "", TextAnchor.MiddleLeft);
        aviso = Etiqueta(Caja("Aviso visual", transform, new Vector2(.5f, 1),
            new Vector2(0, -178), new Vector2(490, 56)), "", TextAnchor.MiddleCenter);
        cajaSubtitulo = Caja("Subtítulos", transform, new Vector2(.5f, 0),
            new Vector2(0, 72), new Vector2(810, 100));
        subtitulo = Etiqueta(cajaSubtitulo, "", TextAnchor.MiddleCenter);
        Boton(transform, "F1 · Accesibilidad", new Vector2(1, 1),
            new Vector2(-140, -42), AlternarMenu);
        opciones = Caja("Opciones de accesibilidad", transform, new Vector2(.5f, .5f),
            Vector2.zero, new Vector2(560, 184));
        primerBoton = Boton(opciones, "", new Vector2(.5f, .5f), new Vector2(0, 33), CambiarDaltonismo, 445);
        textoDaltonismo = primerBoton.GetComponentInChildren<Text>();
        Boton(opciones, "Volver al juego", new Vector2(.5f, .5f), new Vector2(0, -33), AlternarMenu, 445);
        opciones.gameObject.SetActive(false);

        audioAviso = gameObject.AddComponent<AudioSource>();
        audioAviso.playOnAwake = false;
        var muestras = new float[4800];
        for (int i = 0; i < muestras.Length; i++)
        {
            float envolvente = Mathf.Sin(Mathf.PI * i / muestras.Length);
            muestras[i] = .25f * envolvente * Mathf.Sin(2 * Mathf.PI * 660 * i / 24000);
        }
        tono = AudioClip.Create("Aviso de cercanía", muestras.Length, 1, 24000, false);
        tono.SetData(muestras, 0);

        StarCollectible.Recogido += AlRecoger;
        TrapController.Activada += AlActivarTrampa;
        mensaje = "Guía del bosque · Reúne las estrellas, esquiva las trampas y busca el diamante.";
        InicializarTexto();
        ActualizarProgreso();
        AplicarPaleta();
    }

    private void Update()
    {
        var teclado = Keyboard.current;
        if (teclado != null && (teclado.f1Key.wasPressedThisFrame ||
            (menuAbierto && teclado.escapeKey.wasPressedThisFrame)))
            AlternarMenu();
        if (menuAbierto)
            return;

        bool peligro = personaje != null && trampas.Any(trampa => trampa != null &&
            trampa.isActiveAndEnabled && (trampa.transform.position - personaje.transform.position).sqrMagnitude < 16f);
        if (peligro && !cercaDeTrampa)
            audioAviso.PlayOneShot(tono);
        cercaDeTrampa = peligro;

        bool avisoActivo = Time.unscaledTime < avisoHasta;
        aviso.transform.parent.gameObject.SetActive(avisoActivo || peligro);
        if (!avisoActivo && peligro)
            aviso.text = "! Trampa cerca · rodéala o salta";
    }

    private void AlRecoger(bool esEstrella)
    {
        ActualizarProgreso();
        if (!esEstrella)
        {
            objetivo.text = "[✓] Diamante encontrado";
            Mostrar("[✓] Diamante recogido", "Guía del bosque · Encontraste el objeto escondido.");
        }
        else if (juego.StarsCollected >= juego.starsRequired)
        {
            Mostrar("[◇] Apareció el diamante", "Guía del bosque · Ya están todas las estrellas. Busca el diamante.");
        }
        else
        {
            Mostrar("[★] Estrella recogida", "Guía del bosque · Sigue buscando las estrellas que faltan.");
        }
    }

    private void AlActivarTrampa()
    {
        Mostrar("[!] Trampa activada", "Guía del bosque · Aléjate y rodea la trampa.");
    }

    private void Mostrar(string señal, string dialogo)
    {
        aviso.text = señal;
        avisoHasta = Time.unscaledTime + 3f;
        mensaje = dialogo;
        subtitulo.text = mensaje;
    }

    private void ActualizarProgreso()
    {
        int total = juego.starsRequired;
        int cantidad = Mathf.Min(juego.StarsCollected, total);
        progreso.text = $"{cantidad} / {total}";
        objetivo.text = cantidad >= total ? "[◇] Encuentra el diamante" : "[★] Reúne las estrellas";
        for (int i = 0; i < estrellas.Length; i++)
            estrellas[i].sprite = i < cantidad ? estrellaLlena : estrellaVacia;
    }

    private void CambiarDaltonismo()
    {
        daltonismo = !daltonismo;
        AplicarPaleta();
    }

    private void AplicarPaleta()
    {
        textoDaltonismo.text = daltonismo ? "Daltonismo · Activado" : "Daltonismo · Desactivado";
        Color fondo = daltonismo ? Color.black : new Color(.045f, .065f, .09f, .96f);
        progreso.transform.parent.GetComponent<Image>().color = fondo;
        objetivo.transform.parent.GetComponent<Image>().color = fondo;
        aviso.transform.parent.GetComponent<Image>().color = fondo;
        cajaSubtitulo.GetComponent<Image>().color = fondo;
        opciones.GetComponent<Image>().color = fondo;
        objetivo.color = daltonismo ? new Color32(111, 226, 255, 255) : Color.white;
        aviso.color = daltonismo ? new Color32(255, 228, 98, 255) : Color.white;
    }

    private void InicializarTexto()
    {
        foreach (Text etiqueta in GetComponentsInChildren<Text>(true))
            etiqueta.fontSize = 20;
        subtitulo.text = mensaje;
    }

    private void AlternarMenu()
    {
        menuAbierto = !menuAbierto;
        opciones.gameObject.SetActive(menuAbierto);
        if (menuAbierto)
        {
            tiempoAnterior = Time.timeScale;
            bloqueoAnterior = Cursor.lockState;
            cursorVisible = Cursor.visible;
            if (personaje != null)
            {
                cursorBloqueado = personaje.cursorLocked;
                controlarMirada = personaje.cursorInputForLook;
                personaje.cursorLocked = false;
                personaje.cursorInputForLook = false;
                personaje.LookInput(Vector2.zero);
            }
            Time.timeScale = 0;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            EventSystem.current?.SetSelectedGameObject(primerBoton.gameObject);
        }
        else
        {
            EventSystem.current?.SetSelectedGameObject(null);
            RestaurarJuego();
        }
    }

    private void RestaurarJuego()
    {
        Time.timeScale = tiempoAnterior;
        if (personaje != null)
        {
            personaje.cursorLocked = cursorBloqueado;
            personaje.cursorInputForLook = controlarMirada;
        }
        Cursor.lockState = bloqueoAnterior;
        Cursor.visible = cursorVisible;
    }

    private void OnDestroy()
    {
        StarCollectible.Recogido -= AlRecoger;
        TrapController.Activada -= AlActivarTrampa;
        if (menuAbierto)
            RestaurarJuego();
        if (tono != null)
            Destroy(tono);
    }

    private RectTransform Caja(string nombre, Transform padre, Vector2 ancla, Vector2 posicion, Vector2 tamaño)
    {
        var objeto = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        objeto.layer = gameObject.layer;
        var rect = (RectTransform)objeto.transform;
        rect.SetParent(padre, false);
        rect.anchorMin = rect.anchorMax = ancla;
        rect.anchoredPosition = posicion;
        rect.sizeDelta = tamaño;
        var imagen = objeto.GetComponent<Image>();
        imagen.color = new Color(.045f, .065f, .09f, .96f);
        imagen.raycastTarget = false;
        return rect;
    }

    private Text Etiqueta(RectTransform padre, string texto, TextAnchor alineacion)
    {
        var objeto = new GameObject("Texto", typeof(RectTransform), typeof(Text));
        objeto.layer = gameObject.layer;
        var rect = (RectTransform)objeto.transform;
        rect.SetParent(padre, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(12, 4);
        rect.offsetMax = new Vector2(-12, -4);
        var etiqueta = objeto.GetComponent<Text>();
        etiqueta.font = fuente;
        etiqueta.text = texto;
        etiqueta.color = Color.white;
        etiqueta.alignment = alineacion;
        etiqueta.raycastTarget = false;
        return etiqueta;
    }

    private Button Boton(Transform padre, string texto, Vector2 ancla, Vector2 posicion,
        UnityEngine.Events.UnityAction accion, int ancho = 248)
    {
        var rect = Caja(texto.Length == 0 ? "Opción" : texto, padre, ancla, posicion, new Vector2(ancho, 54));
        var imagen = rect.GetComponent<Image>();
        imagen.color = new Color(.22f, .29f, .39f);
        imagen.raycastTarget = true;
        var boton = rect.gameObject.AddComponent<Button>();
        boton.targetGraphic = imagen;
        var colores = boton.colors;
        colores.highlightedColor = colores.selectedColor = new Color(.78f, .9f, 1f);
        boton.colors = colores;
        boton.onClick.AddListener(accion);
        Etiqueta(rect, texto, TextAnchor.MiddleCenter);
        return boton;
    }
}
