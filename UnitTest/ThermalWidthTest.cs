using System;
using DistrictEnergy.Metrics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace UnitTest
{
    [TestClass]
    public class ThermalWidthTest
    {
        [TestMethod]
        public void Width_WithValidAmont_ReturnsResult()
        {
            // arrange
            double area = 100;
            double length = 10;
            double expected = 10;
            double thermalWidth = Metrics.EffThermalWidth(area, length);

            // Asser
            Assert.AreEqual(expected,thermalWidth,0.001,"Thermal Width Not Calculated Correctly");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ThermalWidth_WhenAmountIsLessThanZero_ShouldThrowArgumentOutOfRange()
        {
            
        }
    }
}
