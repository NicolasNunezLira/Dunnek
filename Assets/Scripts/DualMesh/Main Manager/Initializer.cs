using UnityEngine;
using DunefieldModel_DualMesh;
using System.Collections.Generic;
using Building;
using ConstructionSystem;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public partial class DualMesh : MonoBehaviour
{
    public void Initializer()
    {
        constructions = new Dictionary<int, ConstructionInstance>();

        compositeConstructions = new Dictionary<int, WallGroup>();

        constructionGrid = new ConstructionGrid(xResolution + 1, zResolution + 1);

        slopeFinder = new FindSlopeMooreDeterministic();

        // Initialize the terrain and sand meshes
        dualMeshConstructor = new DualMeshConstructor(
            xResolution, zResolution,
            simXResolution, simZResolution,
            size,
            terrainScale1, terrainScale2, terrainScale3,
            terrainAmplitude1, terrainAmplitude2, terrainAmplitude3,
            sandScale1, sandScale2, sandScale3,
            sandAmplitude1, sandAmplitude2, sandAmplitude3,
            terrainMaterial, sandMaterial,
            criticalSlopes, criticalSlopeThreshold,
            ref planicie, this.transform);

        dualMeshConstructor.Initialize(
            out terrainGO, out sandGO,
            out sand, out terrain, out terrainShadow);

        // Initialize the sand mesh to be above the terrain mesh
        sandChanges = new FrameVisualChanges(sand.VisualWidth, sand.VisualHeight);
        terrainShadowChanges = new FrameVisualChanges(terrainShadow.VisualWidth, terrainShadow.VisualHeight);

        // Initialize the desert prefabs
        MountainPrefabSpawner.Instance.Spawn();
        
        VegetationManager.Instance.Spawn();
        
        //RockPrefabSpawner.Instance.Spawn();

        duneModel = new ModelDM(
            slopeFinder,
            sand, terrainShadow, ref terrain,
            constructionGrid,
            size,
            xResolution + 1, zResolution + 1,
            slope,
            //(int)windDirection.x, (int)windDirection.y,
            ref constructions,
            ref currentConstructionID,
            ref currentCompositeConstructionID,
            heightVariation, heightVariation,
            hopLength, shadowSlope, avalancheSlope,
            maxCellsPerFrame,
            conicShapeFactor,
            avalancheTransferRate,
            minAvalancheAmount,
            sandChanges,
            terrainShadowChanges
        );
        duneModel.SetOpenEnded(openEnded);
        duneModel.InitAvalancheQueue();
        grainsForAvalanche = duneModel.avalancheQueue.Count;

        activePreview = PreviewManager.Instance.buildPreviews["houseSand"]["building"];
        currentConstruction = "houseSand";

        builder = new BuildSystem(
            duneModel,
            dualMeshConstructor,
            constructions,
            currentConstructionID,
            currentCompositeConstructionID,
            pulledDownTime,
            inMode,
            currentBuildMode,
            currentConstruction,
            currentActionMode,
            terrain,
            activePreview,
            constructionGrid,
            planicie);
    }

}
