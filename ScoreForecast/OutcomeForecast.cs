using System;
using System.Collections.Generic;
using System.Xml;

namespace ScoreForecast
{
    public enum Outcome
    {
        Host,
        Guest,
        Draw,
        Incomplete
    }

    /// <summary>
    /// Прогноз исхода
    /// </summary>
    public class OutcomePrediction
    {
        /// <summary>
        /// Вероятность победы команды Хозяев
        /// </summary>
        public double Host { get; set; }

        /// <summary>
        /// Вероятность победы команды Гостей
        /// </summary>
        public double Guest { get; set; }

        /// <summary>
        /// Вероятность ничьей
        /// </summary>
        public double Draw { get; set; }

        /// <summary>
        /// Вероятность не закрытого интервала
        /// </summary>
        public double Incomplete { get; set; }

        public override string ToString()
        {
            return $"Вероятность выигрыша хозяев: {Math.Round(Host * 100, 2)} %{Environment.NewLine}Вероятность выигрыша гостей: {Math.Round(Guest * 100, 2)} %{Environment.NewLine}Вероятность ничьей: " +
                $"{Math.Round(Draw * 100, 2)} %{Environment.NewLine}Вероятность не закрытого интервала: {Math.Round(Incomplete * 100, 2)} %";
        }
    }

    public class OutcomeForecast
    {
        private readonly int _remainingTime;
        private readonly Outcome[] _outcomes;
        private readonly int _startGoal;
        private readonly int _endGoal;

        private double _hostLambda = 0;
        private double _guestLambda = 0;
        private double _totalLambda = 0;

        private int _hostGoalCount;
        private int _guestGoalCount;        
        /// <summary>
        /// Количество голов, которые требуется проанализировать на заданном интервале
        /// </summary>
        private int _intervalCount;

        /// <summary>
        /// Количество прогонов для проверки Монте-Карло
        /// </summary>
        private const int _runCount = 100000; // 100K

        private Dictionary<int, double> _goals;
        private readonly Random _random;

        /// <summary>
        /// Вероятность что гол забит командой Хозяев
        /// </summary>
        private double _hostProbability;

        /// <summary>
        /// Вероятность что гол забит командой Гостей
        /// </summary>
        private double _guestProbability;

        public OutcomeForecast(int remainingTime, Outcome[] outcomes, int startGoal, int endGoal)
        {
            _remainingTime = remainingTime;
            _outcomes = outcomes;
            _startGoal = startGoal;
            _endGoal = endGoal;

            _goals = new Dictionary<int, double>();
            _random = new Random();

            Init();
        }

        private void Init()
        {
            XmlDocument xmlDoc = new XmlDocument(); // Create an XML document object
            xmlDoc.Load("StatisticalData.xml"); // Load the XML document from the specified file            
            // Get elements
            XmlNodeList matches = xmlDoc.GetElementsByTagName("match");

            // Ожидаемое количество забитых голов для каждой команды - берется как средневзвешенное значение из статистики (сумма весов равна единице => достаточно просуммировать)
            _hostLambda = 0;
            _guestLambda = 0;
            // double weight = 0;

            foreach (XmlNode item in matches)
            {
                int hostScore = int.Parse(item["host"].InnerText);
                int guestScore = int.Parse(item["guest"].InnerText);
                double rate = double.Parse(item["rate"].InnerText);

                _hostLambda += hostScore * rate;
                _guestLambda += guestScore * rate;
                _totalLambda = _hostLambda + _guestLambda;

                int totalScore = hostScore + guestScore;
                if (_goals.ContainsKey(totalScore))
                    _goals[totalScore] += rate;
                else
                    _goals.Add(totalScore, rate);
            }

            int startIndex = _startGoal > _outcomes.Length ? _startGoal : _outcomes.Length; // фактический стартовый номер гола для вычисления в заданном интервале
            _intervalCount = _endGoal - startIndex + 1; // количество голов в интервале для анализа

            /*
             * Вычисляем уже забитые голы в заданном интервале
             */
            for (int i = _startGoal; i < _outcomes.Length; i++)
            {
                if (_outcomes[i] == Outcome.Host)
                    _hostGoalCount++;
                else
                    _guestGoalCount++;
            }

            // Сколько потребуется времени для того чтобы забить нужное количество голов и сколько времени имеется в наличии
            double requiredTime = (_intervalCount / _totalLambda) * 90d;
            double remainingCoef = _remainingTime / requiredTime;
            _hostLambda *= remainingCoef;
            _guestLambda *= remainingCoef;
            _totalLambda *= remainingCoef;

            _hostProbability = _hostLambda / _totalLambda;
            _guestProbability = _guestLambda / _totalLambda;
        }

        /// <summary>
        /// Вычисляем исход при заданных условиях на основе распределения Пуассона
        /// </summary>
        /// <returns></returns>
        public OutcomePrediction Calculate()
        {
            OutcomePrediction result = new OutcomePrediction();                        

            double incompletionProbability = Poisson.GetCumulativePoisson(_intervalCount, _totalLambda); // вероятность того, что требуемое для закрытия интервала количество голов не будет забито
            result.Incomplete = incompletionProbability;

            int currentHost = _intervalCount;
            int currentGuest = 0;

            /*
             * Рассматриваем все возможные комбинации распределения голов на заданном интервале, начиная с "все голы забиты командой Хозяев" до "все голы забиты командой Гостей"
             * */
            while (currentHost >= 0)
            {
                double p = Poisson.GetPoisson(currentHost, _hostLambda) * Poisson.GetPoisson(currentGuest, _guestLambda); // (count == intervalCount ? 1 : completionProbability);
                int ratio = Math.Sign(_hostGoalCount + currentHost - _guestGoalCount - currentGuest);
                switch (ratio)
                {
                    case 1:
                        result.Host += p;
                        break;
                    case -1:
                        result.Guest += p;
                        break;
                    case 0:
                        result.Draw += p;
                        break;
                }

                currentHost--;
                currentGuest++;
            }

            return result;
        }

        /// <summary>
        /// Моделируем исход при заданных условиях методом Монте-Карло
        /// </summary>
        /// <returns></returns>
        public OutcomePrediction GetTestResult()
        {
            OutcomePrediction result = new OutcomePrediction();           

            int totalCount = _hostGoalCount + _guestGoalCount + _intervalCount;

            int hostWinCount = 0;
            int guestWinCount = 0;
            int drawCount = 0;
            int incompleteCount = 0;

            for (int i = 0; i < _runCount; i++)
            {
                int currentHostGoalCount = _hostGoalCount;
                int currentGuestGoalCount = _guestGoalCount;

                if (!IsIntervalComplete(totalCount))
                {
                    incompleteCount++;
                    continue;
                }

                for (int j = 0; j < _intervalCount; j++)
                {
                    Outcome outcome = GetRandomOutcome();

                    if (outcome == Outcome.Host)
                    {
                        currentHostGoalCount++;
                    }
                    else
                    {
                        currentGuestGoalCount++;
                    }
                }

                int ratio = Math.Sign(currentHostGoalCount - currentGuestGoalCount);
                switch (ratio)
                {
                    case 1:
                        hostWinCount++;
                        break;
                    case -1:
                        guestWinCount++;
                        break;
                    case 0:
                        drawCount++;
                        break;
                }
            }

            result.Host = (double)hostWinCount / _runCount;
            result.Guest = (double)guestWinCount / _runCount;
            result.Draw = (double)drawCount / _runCount;
            result.Incomplete = (double)incompleteCount / _runCount;

            return result;
        }

        /// <summary>
        /// Закрыт ли интервал
        /// </summary>
        /// <param name="count">Количество голов (уже забитых и тех что требуется забить)</param>
        /// <returns></returns>
        private bool IsIntervalComplete(int count)
        {
            double p = _random.NextDouble();

            /*
             * Вычисляем вероятность количества голов не менее трубемого (>= count)
             * */
            double cumulative = 0;
            for (int i = count; i < _goals.Count; i++)
            {
                cumulative += _goals[i];
            }

            return p <= cumulative;
        }

        /// <summary>
        /// Генерируем гол одной из команд с учетом их вероятности
        /// </summary>
        /// <returns></returns>
        private Outcome GetRandomOutcome()
        {
            double p = _random.NextDouble();

            if (_hostProbability > _guestProbability)
            {
                if (p < _hostProbability)
                    return Outcome.Host;
                return Outcome.Guest;
            }
            else
            {
                if (p < _guestProbability)
                    return Outcome.Guest;
                return Outcome.Host;
            }
        }
    }
}
