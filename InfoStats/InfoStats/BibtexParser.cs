using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace InfoStats
{
    /// <summary>
    /// Parses a Bibtex file into C# objects
    /// </summary>
    public class BibtexParser : IDisposable
    {
        /// <summary> The file stream to be read </summary>
        private StreamReader _readingStream;
        /// <summary> File location </summary>
        public string ReadingPath { get; private set; }

        /// <summary>
        /// Class constructor.
        /// </summary>
        /// <param name="filePath">The path of the file to be opened for reading</param>
        public BibtexParser(string filePath)
        {
            ReadingPath = filePath;
        }

        #region Streaming options
        /// <summary>
        /// Checks if there are still available rows to be read from the file.
        /// </summary>
        /// <returns></returns>
        public bool HasNext()
        {
            return IsStreamingOpen() && !(_readingStream.EndOfStream);
        }

        /// <summary>
        /// Returns a boolean indicating whether file stream is already opened
        /// </summary>
        /// <returns></returns>
        public bool IsStreamingOpen()
        {
            return _readingStream != null;
        }

        /// <summary>
        /// Opens the file for reading
        /// </summary>
        public void OpenStreaming()
        {
            // sanity check
            if (IsStreamingOpen())
                throw new InvalidOperationException("File streaming is already opened");

            // initializing the data streaming
            _readingStream = new StreamReader(ReadingPath);
        }

        /// <summary>
        /// Releases all resources used by the stream
        /// </summary>
        public void Close()
        {
            Dispose();
        }


        /// <summary>
        /// Releases all resources used by the stream
        /// </summary>
        public void Dispose()
        {
            if (IsStreamingOpen())
            {
                _readingStream.Dispose();
            }
        }
        #endregion

        /// <summary>
        /// Reads a block of Bibtex references.
        /// </summary>
        /// <param name="blockSize">
        /// The number of Bibtex references. If 'zero', all bibtex registers are read.
        /// </param>
        /// <returns>
        /// A list with size less than or equals blockSize
        /// </returns>
        public List<BibtexRecord> ReadBibtexFile(int blockSize = 0)
        {
            // sanity check
            if (blockSize < 0)
                throw new ArgumentOutOfRangeException("blockSize must be greater than zero");

            // invalidating stop condition
            if (blockSize == 0)
                blockSize = int.MaxValue;

            // list of Bibtex references to be returned 
            List<BibtexRecord> bibtexBlocks = new List<BibtexRecord>();

            // number of Bibtex references read
            int i = 0;
            // reading until reaching block size or file contents end
            while (i < blockSize && HasNext())
            {
                // building and retrieving Bibtex reference object
                BibtexRecord bibtexRecord = GetBibtexObject();

                // sanity check
                if (bibtexRecord == null)
                    return bibtexBlocks;

                // adding to the list of Bibtex objects
                bibtexBlocks.Add(bibtexRecord);
                // counting one more object
                i++;
            }

            return bibtexBlocks;
        }




        /// <summary>
        /// Builds and returns a BibTex object based on the input stream open.
        /// </summary>
        /// <returns></returns>
        private BibtexRecord GetBibtexObject()
        {
            /* '@' indicates a new bibtex record. Searching for it */
            // retrieving current row
            string currentRow = _readingStream.ReadLine();
            // sanity check and condition check
            while (currentRow != null && !currentRow.TrimStart().StartsWith("@"))
            {
                // move forwards
                currentRow = _readingStream.ReadLine();
            }

            // if current row is null, we reached the end of the file
            if (currentRow == null)
                return null;

            // the object to be returned
            BibtexRecord bibtexRecord = new BibtexRecord();

            // the accumulated number of opened braces 
            int accOpenedBraces = currentRow.Count(s => s.Equals('{'));
            // the accumulated number of closed braces 
            int accClosedBraces = currentRow.Count(s => s.Equals('}'));
            // the difference between opened and closed braces
            int bracesDiff = accOpenedBraces - accClosedBraces;

            // The IEEE paper ID is in the same row as '@'
            bibtexRecord.Id = currentRow.TrimEnd(',').Substring(currentRow.IndexOf('{') + 1);

            // reading file row
            currentRow = _readingStream.ReadLine();

            // when bracesDiff reaches zero braces are balanced, 
            // indicating that the current Bibtex record is over     
            while (currentRow != null)
            {
                // removing new line characters
                currentRow = currentRow.Replace(Environment.NewLine, string.Empty);

                string[] rowTokens = currentRow.Split(new char[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);
                // sanity check
                if (rowTokens != null && rowTokens.Length == 2)
                {
                    // the property name
                    string propertyName = rowTokens[0];
                    // the property value
                    string propertyValue = rowTokens[1].TrimStart('{').Replace("},", string.Empty);

                    // using reflection
                    PropertyInfo prop = bibtexRecord.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (prop != null && prop.CanWrite)
                        prop.SetValue(bibtexRecord, propertyValue);

                }
                else
                {
                    // sanity check error thrown
                    throw new InvalidOperationException("I faced either an invalid row token or a row token with unexpected size.");
                }
                // accumulating opened and closed braces
                accOpenedBraces += currentRow.Count(s => s.Equals('{'));
                accClosedBraces += currentRow.Count(s => s.Equals('}'));
                // calculating the difference
                bracesDiff = accOpenedBraces - accClosedBraces;

                if (bracesDiff == 0)
                    break;

                // moving forwards
                currentRow = _readingStream.ReadLine();
            }

            // sanity check
            if (currentRow == null && bracesDiff != 0)
                throw new InvalidOperationException("File ended before braces diff reached zero");


            return bibtexRecord;
        }

    }
}
