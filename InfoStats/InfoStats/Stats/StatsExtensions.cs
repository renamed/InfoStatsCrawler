using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoStats.Stats
{
    public static class StatsExtensions
    {
        /// <summary>
        /// Calculates the standard deviation of the list values
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static double StdDev(this IEnumerable<double> values)
        {
            return Math.Sqrt(Var(values));
        }
        /// <summary>
        /// Calculates the variance of the list values
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static double Var(this IEnumerable<double> values)
        {
            double ret = 0;
            int count = values.Count();
            if (count > 1)
            {
                //Compute the Average
                double avg = values.Average();

                //Perform the Sum of (value-avg)^2
                double sum = values.Sum(d => (d - avg) * (d - avg));

                //Put it all together
                ret = sum / count;
            }
            return ret;
        }


        /// <summary>
        /// Calculates the standard deviation of the list values
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static double StdDev(this IEnumerable<int> values)
        {
            return Math.Sqrt(Var(values));
        }
        /// <summary>
        /// Calculates the variance of the list values
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public static double Var(this IEnumerable<int> values)
        {
            double ret = 0;
            int count = values.Count();
            if (count > 1)
            {
                //Compute the Average
                double avg = values.Average();

                //Perform the Sum of (value-avg)^2
                double sum = values.Sum(d => (d - avg) * (d - avg));

                //Put it all together
                ret = sum / count;
            }
            return ret;
        }
    }
}
