using UnityEngine;
using Deform;
using System;

public class PulledDown : MonoBehaviour
{
    [Header("Configuaración de colapso")]
    public bool activatePulledDown = false;
    public float collapseSpeed = 0.2f;        // Qué tan rápido colapsa
    
    // Deformers
    private TaperDeformer taper;
    private BulgeDeformer bulge;
    private SpherifyDeformer spherify;

    private float initialTime;

    public float Duration => 1f / collapseSpeed;

    public float CollapseProgress => Mathf.Clamp01((Time.time - initialTime) * collapseSpeed);
    public float CurrentHeight => transform.localScale.y;
    public bool IsCollapsing => isCollapsing;

    private bool isCollapsing = false;


    void Start()
    {
        // Get deformers components
        taper = GetComponentInChildren<TaperDeformer>();

        bulge = GetComponentInChildren<BulgeDeformer>();

        spherify = GetComponentInChildren<SpherifyDeformer>();

        initialTime = Time.time;

        // Estado inicial
        // BulgeDeformer
        bulge.Factor = 0f;

        // TaperDeformer
        taper.TopFactor = new Vector2(1f, 1f);
        taper.BottomFactor = new Vector2(1f, 1f);

        // SpherifyDeformer
        spherify.Factor = 0f;
        spherify.Radius = 2f;
        spherify.transform.localPosition = new Vector3(0f, 0.55f, 0f);
    }

    void Update()
    {
        if (activatePulledDown && !isCollapsing)
        {
            initialTime = Time.time;
            isCollapsing = true;            
        }
        if (!isCollapsing) return;

        float t = (Time.time - initialTime) * collapseSpeed;
        t = Mathf.Clamp01(t); // Asegurarse que esté entre 0 y 1

        
        float topFactor = Mathf.Max(1f - t, 0.8f);
        float bottomFactor = Mathf.Min(1f + t, 1.2f);
        taper.TopFactor = new Vector2(topFactor, topFactor);
        taper.BottomFactor = new Vector2(bottomFactor, bottomFactor);

        bulge.Factor = Mathf.Min(0.22f, t);

        transform.localScale = new Vector3(1f, Mathf.Max(1f - t, 0.4f), 1f);

        spherify.Factor = Mathf.Min(0.35f, t);
    }
}

