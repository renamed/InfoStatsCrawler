using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoStats
{
    class Program
    {
        static void Main(string[] args)
        {
            string jsonFilePath = ConfigurationManager.AppSettings["JsonFilePath"];
        }

        #region "How works related to "Convolutional Neural Networks" have been involving along the years?"
        static StatsResult PapersByYear()
        {

        }
        #endregion
    }

    class StatsResult
    {
        public int Year { get; set; }
        public double Value { get; set; }
    }
}
