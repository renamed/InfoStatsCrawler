using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoStats.Stats
{
    /// <summary>
    /// Citations / All Citations by year (avg and stddev) - total citations from the last 3 years
    /// </summary>
    public class PapersByYearAvgStdByYear
    {
        /// <summary>
        /// The year of reference
        /// </summary>
        public string Year { get; set; }
        /// <summary>
        /// The year's average
        /// </summary>
        public double Avg { get; set; }
        /// <summary>
        /// The year's std dev
        /// </summary>
        public double StdDev { get; set; }
    }
}
