namespace DunefieldModel_DualMesh
{
    public partial class ModelDM
    {
        public virtual void DepositGrain(int x, int z, int dx, int dz, float depositeHeight)
        {
            while (FindSlope.Downslope(x, z, dx, dz, out int xLow, out int zLow) >= 1)
            {
                x = xLow;
                z = zLow;
            }

            if (terrainShadow[x, z] >= sand[x, z])
            {
                sand[x, z] = terrainShadow[x, z] + depositeHeight;
            }
            else
            {
                sand[x, z] += depositeHeight;
            }

            UpdateShadow(x, z, dx, dz);

            ActivateCell(x, z);          
        }

    }
}
