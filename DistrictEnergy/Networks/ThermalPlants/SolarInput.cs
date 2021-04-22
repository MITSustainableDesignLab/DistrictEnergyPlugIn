namespace DistrictEnergy.Networks.ThermalPlants
{
    public abstract class SolarInput : NotStorage
    {
        public abstract double[] SolarAvailableInput(int t = 0, int dt = 8760);
    }
}