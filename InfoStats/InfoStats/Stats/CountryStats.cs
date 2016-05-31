using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoStats.Stats
{
    /// <summary>
    /// Country statistics
    /// </summary>
    public class CountryStats
    {
        /// <summary>
        /// Country whose statistics belong to
        /// </summary>
        public string Country       { get; set; }
        /// <summary>
        /// The average measures the balance point of a numeric series
        /// </summary>
        public double Avg           { get; set; }
        /// <summary>
        /// Standard deviation measures how numeric points are distributed around the avg
        /// </summary>
        public double StdDev        { get; set; }
        /// <summary>
        /// Variance measures how fast numeric points distance from the avg
        /// </summary>
        public double Var           { get; set; }
        /// <summary>
        /// The mean is the value that divides the time series into two
        /// </summary>
        public double Mean          { get; set; }
        /// <summary>
        /// The highest value of a series
        /// </summary>
        public double HighestValue { get; set; }
        /// <summary>
        /// The lowest value of a series
        /// </summary>
        public double LowestValue { get; set; }        
        /// <summary>
        /// Mode measures the value that appears the most in a numeric series
        /// </summary>
        public int    Mode          { get; set; } 
        /// <summary>
        /// The median point is the average between the highest and lowest values
        /// </summary>
        public double MedianPoint   { get; set; }

        /*       
        /// <summary>
        /// The value that divides the series in 25%.
        /// That is, 25% of the series values are below this value.
        /// </summary>
        public double FirstQuatile  { get; set; }
        /// <summary>
        /// The value that divides the series in 50%.
        /// That is, 50% of the series values are below this value.
        /// </summary>
        public double SecondQuatile { get; set; }
        /// <summary>
        /// The value that divides the series in 75%.
        /// That is, 75% of the series values are below this value.
        /// </summary>
        public double ThirdQuatile  { get; set; } */
    }
}
