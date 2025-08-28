using UnityEngine;
using System.Collections.Generic;
using Utils;

namespace StormSystem
{
    public class StormManager : Singleton<StormManager>
    {
        public string configFile = "Configs/StormsProperties";
        private StormConfig config;

        private int currentStormIndex = 0;
        private int currentEventIndex = 0;
        public int eventTurnsRemaining { get; private set; } 
        public Vector2 currentDirection { get; private set; }
        private float currentMagnitude;

        #region Awake
        protected override void Awake()
        {
            base.Awake();
            LoadConfig();
            InitStorm(0);

            TimeManager.Instance.OnTimeAdvance += AdvanceTurn;
        }
        #endregion

        #region Load Configurations
        private void LoadConfig()
        {
            TextAsset jsonFile = Resources.Load<TextAsset>(configFile);
            if (jsonFile == null)
            {
                Debug.LogError($"No se encontro el archivo de tormentas en Resources/+{configFile}.json");
                return;
            }

            config = JsonUtility.FromJson<StormConfig>(jsonFile.text);
            Debug.Log("Tormentas cargadas:" + config.storms.Count);
            Debug.Log("Multiplicadores cargados:" + config.multipliers.Count);
        }
        #endregion

        #region Getters
        public StormData GetStorm(int index)
        {
            if (index < 0 || index >= config.storms.Count)
            {
                return null;
            }
            return config.storms[index];
        }

        public StormMultiplier GetMultiplier(int index)
        {
            if (index < 0 || index >= config.multipliers.Count)
            {
                return null;
            }
            return config.multipliers[index];
        }
        #endregion

        #region Init Storm
        private void InitStorm(int index)
        {
            StormData storm = config.storms[index];
            currentEventIndex = 0;
            eventTurnsRemaining = storm.ta;
            currentMagnitude = storm.a;
            currentDirection = Random.insideUnitCircle.normalized;
        }
        #endregion

        #region Advance Turn
        public void AdvanceTurn()
        {
            if (config == null || config.storms.Count == 0) return;

            eventTurnsRemaining--;

            if (eventTurnsRemaining <= 0)
            {
                StormData storm = config.storms[currentStormIndex];

                if (currentEventIndex < storm.na - 1)
                {
                    currentEventIndex++;
                    eventTurnsRemaining = storm.ta;
                    currentMagnitude = storm.a;
                }
                else if (currentEventIndex == storm.na - 1)
                {
                    currentEventIndex++;
                    eventTurnsRemaining = storm.tb;
                    currentMagnitude = storm.b;
                }
                else
                {
                    currentStormIndex = (currentStormIndex + 1) % config.storms.Count;
                    InitStorm(currentStormIndex);
                }

                Vector2 newDir;
                do
                {
                    newDir = Random.insideUnitCircle.normalized;
                } while (Mathf.Abs(Vector2.Angle(currentDirection, newDir)) >= 90f);
                currentDirection = newDir;
                OnWindChanged?.Invoke(currentDirection);
            }
        }
        #endregion

        #region GetWindDirection
        public Vector2Int GetWindDirection()
        {
            return new Vector2Int(
                Mathf.RoundToInt(currentDirection.x * currentMagnitude),
                Mathf.RoundToInt(currentDirection.y * currentMagnitude)
            );
        }
        #endregion

        #region Event for UI
        public event System.Action<Vector2> OnWindChanged;
        #endregion
    }
}