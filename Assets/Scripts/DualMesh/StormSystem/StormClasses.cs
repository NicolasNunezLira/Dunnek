using System.Collections.Generic;
using UnityEngine;

namespace StormSystem
{
    #region Definition of a storm
    [System.Serializable]
    public class StormData
    {
        public float a;
        public float b;
        public int ta;
        public int tb;
        public int na;

        public StormData(
            float a,
            float b,
            int ta,
            int tb,
            int na
        )
        {
            this.a = a;
            this.b = b;
            this.ta = ta;
            this.tb = tb;
            this.na = na;
        }

        public StormData Clone() => new StormData(a, b, ta, tb, na);
    }
    #endregion

    #region Multiplier for a storm
    [System.Serializable]
    public class StormMultiplier
    {
        public float ma, mb, mta, mtb, mna;

        public StormMultiplier(
            float ma,
            float mb,
            float mta,
            float mtb,
            float mna
        )
        {
            this.ma = ma;
            this.mb = mb;
            this.mta = mta;
            this.mtb = mtb;
            this.mna = mna;
        }
    }
    #endregion

    #region Configs
    public class StormConfig
    {
        public List<StormData> storms;
        public List<StormMultiplier> multipliers;
    }
    #endregion
}