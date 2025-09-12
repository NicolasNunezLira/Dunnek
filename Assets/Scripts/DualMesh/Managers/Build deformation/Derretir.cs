using System.Collections.Generic;
using UnityEngine;

public class DerretirConMasa : MonoBehaviour
{
    public float rapidezColapso = 0.01f;
    public float tiempoEntreTicks = 0.05f;
    public float radioRedistribucion = 1.5f;  // Qué tan lejos llega la redistribución de masa
    public float factorExpansión = 0.1f;      // Cuánto se expande lateralmente la base

    private float nextUpdateTime = 0f;
    private Mesh mesh;
    private Vector3[] verticesOriginales;
    private Vector3[] verticesActuales;

    void Start()
    {
        MeshFilter mf = transform.GetChild(0).GetComponent<MeshFilter>();
        mesh = mf.mesh;
        verticesOriginales = mesh.vertices;
        verticesActuales = mesh.vertices;
    }

    void Update()
    {
        if (Time.time >= nextUpdateTime)
        {
            SimularDerrumbeConMasa();
            nextUpdateTime = Time.time + tiempoEntreTicks;
        }
    }

    void SimularDerrumbeConMasa()
    {
        int count = verticesActuales.Length;
        float[] perdidaAltura = new float[count];

        // 1. Calcular pérdida de altura para cada vértice alto
        for (int i = 0; i < count; i++)
        {
            Vector3 v = verticesActuales[i];
            if (v.y > 0.01f)
            {
                float deltaY = rapidezColapso * Mathf.Clamp01(v.y);
                perdidaAltura[i] = deltaY;
                v.y -= deltaY;
                verticesActuales[i] = v;
            }
        }

        // 2. Redistribuir la "masa" perdida a los vértices bajos
        for (int i = 0; i < count; i++)
        {
            if (perdidaAltura[i] <= 0f) continue;

            Vector3 origen = verticesActuales[i];
            float masa = perdidaAltura[i];

            // Buscar vértices más bajos cercanos para expandirlos
            for (int j = 0; j < count; j++)
            {
                if (i == j) continue;

                Vector3 destino = verticesActuales[j];

                // Solo redistribuir hacia vértices más bajos
                if (destino.y > origen.y) continue;

                float distanciaXZ = Vector2.Distance(
                    new Vector2(origen.x, origen.z),
                    new Vector2(destino.x, destino.z)
                );

                if (distanciaXZ < radioRedistribucion)
                {
                    float peso = Mathf.Exp(-Mathf.Pow(distanciaXZ / radioRedistribucion, 2)); // Distribución gaussiana
                    float expansion = masa * peso * factorExpansión;

                    // Expandir lateralmente (en X y Z)
                    Vector3 direccion = (destino - origen).normalized;
                    destino.x += direccion.x * expansion;
                    destino.z += direccion.z * expansion;

                    verticesActuales[j] = destino;
                }
            }
        }

        mesh.vertices = verticesActuales;
        mesh.RecalculateNormals();
    }
}
