using InfoStats.Stats;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
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
                        Parallel.ForEach(
                            currentBlock,
                            new ParallelOptions() { MaxDegreeOfParallelism = 4 },
                            currentBibtex =>
                            {
                                try
                                {
                                    // crawling the IEEE web page and collecting more info about the papers
                                    EnrichPaperInfo enrichPaperInfo = new EnrichPaperInfo();
                                    enrichPaperInfo.EnrichObjectInfo(currentBibtex);
                                }
                                catch (Exception err)
                                {
                                    Console.WriteLine(err.Message);
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
        /// Phase 2 is responsible for calculating statistics from the Phase 1 result
        /// <seealso cref="Phase1()"/>
        /// </summary>
        static void Phase2()
        {
            try
            {
                // retrieving json file path
                string jsonFile = ConfigurationManager.AppSettings["JsonFilePath"];

                // sanity check
                if (string.IsNullOrWhiteSpace(jsonFile))
                    throw new ArgumentNullException();

                // reading the record file
                List<BibtexRecord> paperRecords = JsonConvert.DeserializeObject<List<BibtexRecord>>(File.ReadAllText(jsonFile));

                // Specifying missing values for country and year
                Parallel.For(0,
                    paperRecords.Count,
                    new ParallelOptions() { MaxDegreeOfParallelism = Environment.ProcessorCount * 2 },
                    i =>
                    {
                        if (string.IsNullOrWhiteSpace(paperRecords[i].Country))
                            paperRecords[i].Country = "Não Especificado";
                        if (string.IsNullOrWhiteSpace(paperRecords[i].Year))
                            paperRecords[i].Year = "Não Especificado";
                    });
                            


                PapersStatistics stats = new PapersStatistics(paperRecords);
                //stats.CountPapersByYear();
                //stats.GetCitationByOverallCitationsInYear();
                //stats.CountDistinctCountriesByYear();
                //stats.GetStepsFromStdDevByCountry(10, 3);
                stats.GetCountriesWithMostPublishing(10, 3);
                stats.GetStats(10, 3);
                var abc = stats.CountKeywords(100, 3);
                abc.ToList();
            }
            catch (Exception err)
            {
                Console.WriteLine("\n\n\tAn error has ocurred: " + err.Message);
            }

        }
    }
}
