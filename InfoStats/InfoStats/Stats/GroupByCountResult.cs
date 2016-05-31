using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InfoStats.Stats
{
    /// <summary>
    /// Represents how many times a particular entity has been counted in a particular year
    /// </summary>
    public class GroupByCountResult
    {
        /// <summary>
        /// The grouping value
        /// </summary>
        public string Grouping  { get; set; }
        /// <summary>
        /// The count value
        /// </summary>
        public int Count        { get; set; }
    }
}
