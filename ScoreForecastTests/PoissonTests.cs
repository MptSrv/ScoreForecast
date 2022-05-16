using Microsoft.VisualStudio.TestTools.UnitTesting;
using ScoreForecast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ScoreForecast.Tests
{
    [TestClass()]
    public class PoissonTests
    {
        [TestMethod()]
        public void GetPoissonTest()
        {
            // Arrange
            int x = 2;
            double lambda = 1.7;
            double expected = 0.26398;
            // Проверка на онлайн-калькуляторе: https://stattrek.com/online-calculator/poisson.aspx

            // Act
            double poisson = Math.Round(Poisson.GetPoisson(x, lambda), 5);

            // Assert
            Assert.AreEqual(expected, poisson);
        }

        [TestMethod()]
        public void GetCumulativePoissonTest()
        {
            // Arrange
            int x = 4;
            double lambda = 1.7;
            double expected = 0.9068;
            // Проверка на онлайн-калькуляторе: https://stattrek.com/online-calculator/poisson.aspx

            // Act
            double poisson = Math.Round(Poisson.GetCumulativePoisson(x, lambda), 4);

            // Assert
            Assert.AreEqual(expected, poisson);
        }
    }
}