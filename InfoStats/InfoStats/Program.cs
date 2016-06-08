using InfoStats.Db;
using InfoStats.Stats;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace InfoStats
{
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                // what we should do
                string phase = ConfigurationManager.AppSettings["Phase"];

                // sanity check
                if (string.IsNullOrWhiteSpace(phase))
                {
                    Console.WriteLine("There is no Phase value in App.config");
                    return;
                }

                // checking if we should enrich or do some statistics
                if (phase.Equals("1"))
                    Phase1();
                else if (phase.Equals("2"))
                    Phase2();
                else
                    throw new ArgumentOutOfRangeException("Phase values must be 1 (for enriching) and 2 (for statistics)");
            }
            catch (Exception err)
            {
                if (err != null)
                    Console.WriteLine(err.Message);
                else
                    Console.WriteLine("An error has occurred");
            }
        }

        /// <summary>
        /// Phase 1 is responsible for enriching papers information using
        /// the IEEE web site
        /// </summary>
        static void Phase1()
        {
            try
            {
                // Bibtex file input path
                string inputPath = ConfigurationManager.AppSettings["BibtexFilePath"];

                // Json file path
                string outputPath = ConfigurationManager.AppSettings["JsonFilePath"];

                // how many block sizes we'll be read at once from the Bibtex file
                int blockSize;
                int.TryParse(ConfigurationManager.AppSettings["BlockSize"], out blockSize);

                // check if we should delete the json output file before initiating
                bool deletePrevious;
                bool.TryParse(ConfigurationManager.AppSettings["DeletePreviousFile"], out deletePrevious);

                // checking if we should delete the previous file
                if (File.Exists(outputPath) && deletePrevious)
                    File.Delete(outputPath);

                List<BibtexRecord> allRecords = new List<BibtexRecord>();

                // initializing the object to handle the input file
                using (BibtexParser bibtex = new BibtexParser(inputPath))
                {
                    // opening the file
                    bibtex.OpenStreaming();

                    // checking if we can get further values
                    while (bibtex.HasNext())
                    {
                        // retrieving current Bibtex references block
                        List<BibtexRecord> currentBlock = bibtex.ReadBibtexFile(blockSize);

                        // visiting two web pages at once

                        int total = currentBlock.Count;
                        int qtd = 0;
                        object locked = new object();
                        Parallel.For(0, total, new ParallelOptions() { MaxDegreeOfParallelism = 2 }, (i) =>
                        {                            
                            try
                            {
                                BibtexRecord currentBibtex = currentBlock[i];

                                int seed = DateTime.Now.Millisecond * Guid.NewGuid().GetHashCode();
                                Random r = new Random(seed);

                                int sleepTime = r.Next(200, 600);
                                Thread.Sleep(sleepTime);

                                // crawling the IEEE web page and collecting more info about the papers
                                EnrichPaperInfo enrichPaperInfo = new EnrichPaperInfo();
                                enrichPaperInfo.EnrichObjectInfo(currentBibtex);
                            }
                            catch (Exception err)
                            {
                                Console.WriteLine(err.Message);
                            }
                            finally
                            {
                                lock (locked) { qtd++; }
                                if (qtd % 100 == 0)
                                {
                                    Console.WriteLine("\n\t Feito: {0} de {1} - {2:0.00} % \n", qtd, total, ((1.0 * qtd / total) * 100));
                                }
                            }
                        });
                            
                        allRecords.AddRange(currentBlock);
                    }
                }

                File.WriteAllText(outputPath, JsonConvert.SerializeObject(allRecords));
            }
            catch (Exception err)
            {
                Console.WriteLine("\n\n\tAn error has ocurred: " + err.Message);
            }
        }

        /// <summary>
        /// Phase 2 takes the Json response from Phase 1 and stores in a database
        /// </summary>
        /*static void Phase2()
        {

        }*/

        /// <summary>
        /// Phase 2 is responsible for calculating statistics from the Phase 1 result
        /// <seealso cref="Phase1()"/>
        /// </summary>
        static void Phase2()
        {
            try
            {
                CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("en-US");

                // retrieving json file path
                //string jsonFile = ConfigurationManager.AppSettings["JsonFilePath"];

                // location where the statistics will be saved
                string statisticsResult = ConfigurationManager.AppSettings["StatisticsResult"];

                // sanity check
                if (string.IsNullOrWhiteSpace(statisticsResult))
                    throw new ArgumentNullException();
                /*
                // reading the record file
                List<BibtexRecord> paperRecords = JsonConvert.DeserializeObject<List<BibtexRecord>>(File.ReadAllText(jsonFile));

                // Specifying missing values for country and year
                Parallel.For(0,
                    paperRecords.Count,
                    new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 },
                    i =>
                    {
                        if (string.IsNullOrWhiteSpace(paperRecords[i].Country))
                            paperRecords[i].Country = "Not Specified";
                        if (string.IsNullOrWhiteSpace(paperRecords[i].Year))
                            paperRecords[i].Year = "Not Specified";
                    });
                    */

                //DbAccess dbAccess = new DbAccess();
                //dbAccess.Insert(paperRecords);

                DbAccess dbAccess = new DbAccess();
                List<BibtexRecord> paperRecords = dbAccess.GetAllRecords();

                PapersStatistics stats = new PapersStatistics(paperRecords);
                using (StreamWriter sw = new StreamWriter(statisticsResult, false))
                {
                    // number of papers by year
                    foreach (GroupByCountResult currentRecord in stats.CountPapersByYear())
                    {
                        sw.WriteLine("{0};{1}", currentRecord.Grouping, currentRecord.Count);
                    }

                    // avg and stddev of impact factor by year
                    sw.WriteLine(Environment.NewLine);
                    foreach (PapersByYearAvgStdByYear currentRecord in stats.GetCitationByOverallCitationsInYear())
                    {
                        sw.WriteLine("{0};{1};{2}", currentRecord.Year, currentRecord.Avg, currentRecord.StdDev);
                    }

                    // avg and stddev of citations by year
                    sw.WriteLine(Environment.NewLine);
                    foreach (PapersByYearAvgStdByYear currentRecord in stats.GetAvgCitationsByYear())
                    {
                        sw.WriteLine("{0};{1};{2}", currentRecord.Year, currentRecord.Avg, currentRecord.StdDev);
                    }

                    // avg and stddev of visualizations by year
                    sw.WriteLine(Environment.NewLine);
                    foreach (PapersByYearAvgStdByYear currentRecord in stats.GetAvgVisualizationsByYear())
                    {
                        sw.WriteLine("{0};{1};{2}", currentRecord.Year, currentRecord.Avg, currentRecord.StdDev);
                    }


                    // avg and stddev of visualizations by year
                    sw.WriteLine(Environment.NewLine);
                    foreach (GroupByCountResult currentRecord in stats.CountDistinctCountriesByYear())
                    {
                        sw.WriteLine("{0};{1}", currentRecord.Grouping, currentRecord.Count);
                    }

                    // steps from std dev
                    sw.WriteLine(Environment.NewLine);
                    foreach (GroupByCountResult currentRecord in stats.GetStepsFromStdDevByCountry(20, 3))
                    {
                        sw.WriteLine("{0};{1}", currentRecord.Grouping, currentRecord.Count);
                    }

                    // countries wth most publishing
                    sw.WriteLine(Environment.NewLine);
                    foreach (KeyValuePair<string, int> currentRecord in stats.GetCountriesWithMostPublishing(10, 3))
                    {
                        sw.WriteLine(string.Format("{0};{1}", currentRecord.Key, currentRecord.Value));
                    }

                    // countries statistics
                    sw.WriteLine(Environment.NewLine);
                    foreach (CountryStats currentRecord in stats.GetStats(10, 3))
                    {
                        sw.WriteLine(string.Format("{0};{1};{2};{3};{4};{5};{6};{7};{8}", currentRecord.Country, currentRecord.Avg, currentRecord.StdDev, currentRecord.Var, currentRecord.HighestValue, currentRecord.LowestValue, currentRecord.MedianPoint, currentRecord.Mean, currentRecord.Mode));
                    }

                    // keywords count
                    sw.WriteLine(Environment.NewLine);
                    foreach (KeyValuePair<string, int> currentRecord in stats.CountKeywords(100, 3))
                    {
                        sw.WriteLine(string.Format("{0};{1}", currentRecord.Key, currentRecord.Value));
                    }

                    // Papers by month in 2015
                    sw.WriteLine(Environment.NewLine);
                    foreach (KeyValuePair<string, int> currentRecord in stats.GetPapersByMonth("2015"))
                    {
                        sw.WriteLine(string.Format("{0};{1}", currentRecord.Key, currentRecord.Value));
                    }

                    // Papers by month in 2016
                    sw.WriteLine(Environment.NewLine);
                    foreach (KeyValuePair<string, int> currentRecord in stats.GetPapersByMonth("2016"))
                    {
                        sw.WriteLine(string.Format("{0};{1}", currentRecord.Key, currentRecord.Value));
                    }

                    // China papers by month in 2015
                    sw.WriteLine(Environment.NewLine);
                    foreach (KeyValuePair<string, int> currentRecord in stats.GetPapersByMonth("2015", "China"))
                    {
                        sw.WriteLine(string.Format("{0};{1}", currentRecord.Key, currentRecord.Value));
                    }

                    // Papers by month in 2016
                    sw.WriteLine(Environment.NewLine);
                    foreach (KeyValuePair<string, int> currentRecord in stats.GetPapersByMonth("2016", "China"))
                    {
                        sw.WriteLine(string.Format("{0};{1}", currentRecord.Key, currentRecord.Value));
                    }
                    
                }
            }
            catch (Exception err)
            {
                Console.WriteLine("\n\n\tAn error has ocurred: " + err.Message);
            }

        }
    }
}
