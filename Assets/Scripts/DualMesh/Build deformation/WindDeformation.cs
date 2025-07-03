using UnityEngine;
using Deform;
using System;

public class DeformarCastillo : MonoBehaviour
{
    public float rapidezColapso = 0.2f;        // Qué tan rápido colapsa
    public float alturaInicial = 1f;           // Escala Y inicial del objeto
    public float alturaMinima = 0.1f;          // Altura mínima al final
    public float taperMaximo = 1f;             // Máximo valor de Factor (más positivo = más ancho abajo)

    private TaperDeformer taper;
    private BulgeDeformer bulge;

    private Transform deformTarget;
    private float tiempoInicial;

    void Start()
    {
        // Suponemos que el TaperDeformer está en el hijo del objeto
        taper = GetComponentInChildren<TaperDeformer>();
        deformTarget = taper.transform;

        bulge = GetComponentInChildren<BulgeDeformer>();

        bulge.Factor = 0f;

        tiempoInicial = Time.time;

        // Estado inicial
        taper.TopFactor = new Vector2(1f, 1f);
        taper.BottomFactor = new Vector2(1f, 1f);
        //taper.Factor = 0f; // Comienza sin expansión cónica
        //deformTarget.localScale = new Vector3(1f, alturaInicial, 1f);
    }

    void Update()
    {
        float t = (Time.time - tiempoInicial) * rapidezColapso;
        t = Mathf.Clamp01(t); // Asegurarse que esté entre 0 y 1

        // Escala vertical del objeto (derrumbándose)
        //float nuevaAltura = Mathf.Lerp(alturaInicial, alturaMinima, t);
        //deformTarget.localScale = new Vector3(1f, nuevaAltura, 1f);

        // Aumentar la base del objeto (expansión cónica)
        float topFactor = Mathf.Max((1f - t), 0.8f);
        float bottomFactor = Mathf.Min((1f + t), 1.2f);
        taper.TopFactor = new Vector2(topFactor, topFactor);
        taper.BottomFactor = new Vector2(bottomFactor, bottomFactor);

        bulge.Factor = Mathf.Min(0.22f, t);

        transform.localScale = new Vector3(1f, Mathf.Max(1f - t, 0.6f), 1f);
    }
}

