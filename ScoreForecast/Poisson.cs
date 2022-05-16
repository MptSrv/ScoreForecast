using System;

namespace ScoreForecast
{
    public static class Poisson
    {
        private static int GetFactorial(int n)
        {
            if (n == 0)
                return 1;
            return n * GetFactorial(n - 1);
        }

        public static double GetPoisson(int x, double lambda)
        {
            return (Math.Pow(lambda, x) * Math.Pow(Math.E, -lambda)) / GetFactorial(x);
        }

        public static double GetCumulativePoisson(int x, double lambda)
        {
            double result = 0;

            for (int i = 0; i < x; i++)
            {
                result += GetPoisson(i, lambda);
            }

            return result;
        }
    }
}
