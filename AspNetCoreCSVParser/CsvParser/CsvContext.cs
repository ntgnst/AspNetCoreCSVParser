using System;
using System.Collections.Generic;
using System.IO;

namespace AspNetCoreCSVParser
{
    public class CsvContext
    {
        public IEnumerable<T> Read<T>(string fileName, CsvFileDescription fileDescription) where T : class, ICsvRow, new()
        {
            IEnumerable<T> ie = ReadData<T>(fileName, null, fileDescription);
            return ie;
        }

        public IEnumerable<T> Read<T>(StreamReader stream) where T :  class, ICsvRow, new()
        {
            return Read<T>(stream, new CsvFileDescription());
        }

        public IEnumerable<T> Read<T>(string fileName) where T : class, ICsvRow, new()
        {
            return Read<T>(fileName, new CsvFileDescription());
        }

        public IEnumerable<T> Read<T>(StreamReader stream, CsvFileDescription fileDescription) where T :  class, ICsvRow, new()
        {
            return ReadData<T>(null, stream, fileDescription);
        }


        private IEnumerable<T> ReadData<T>(
                    string fileName,
                    StreamReader stream,
                    CsvFileDescription fileDescription) where T : class , ICsvRow, new()
        {
           
            bool readingRawDataRows = typeof(IDataRow).IsAssignableFrom(typeof(T));

            
            FieldMapper_Reading<T> fm = null;

            if (!readingRawDataRows)
            {
                fm = new FieldMapper_Reading<T>(fileDescription, fileName, false);
            }

           

            bool readingFile = !string.IsNullOrEmpty(fileName);

            if (readingFile)
            {
               stream = new StreamReader(new FileStream(fileName, FileMode.Open, FileAccess.Read));
            }
            else
            {

                if ((stream == null) || (!stream.BaseStream.CanSeek))
                {
                    throw new BadStreamException();
                }

                stream.BaseStream.Seek(0, SeekOrigin.Begin);
            }
            

            CsvStream cs = new CsvStream(stream, null, fileDescription.SeparatorChar);

           
            IDataRow row = null;
            if (readingRawDataRows)
            {
                row = new T() as IDataRow;
            }
            else
            {
                row = new DataRow();
            }

            AggregatedException ae =
                new AggregatedException(typeof(T).ToString(), fileName, fileDescription.MaximumNbrExceptions);

            try
            {
                bool firstRow = true;
                while (cs.ReadRow(ref row))
                {
                   
                    if ((row.Count == 1) &&
                        ((row[0].Value == null) ||
                         (string.IsNullOrEmpty(row[0].Value.Trim()))))
                    {
                        continue;
                    }

                    if (firstRow && fileDescription.FirstLineHasColumnNames)
                    {
                        if (!readingRawDataRows) { fm.ReadNames(row); }
                    }
                    else
                    {
                        T obj = default(T);
                        try
                        {
                            if (readingRawDataRows)
                            {
                                obj = row as T;
                            }
                            else
                            {
                                obj = fm.ReadObject(row, ae);
                            }
                        }
                        catch (AggregatedException ae2)
                        {
                         
                            throw ae2;
                        }
                        catch (Exception e)
                        {
                           
                            ae.AddException(e);
                        }

                        yield return obj;
                    }
                    firstRow = false;
                }
            }
            finally
            {
                if (readingFile)
                {
                    stream.Dispose();
                }

               
                ae.ThrowIfExceptionsStored();
            }
        }

      
        public void Write<T>(
            IEnumerable<T> values,
            string fileName,
            CsvFileDescription fileDescription)
        {
            using (StreamWriter sw = new StreamWriter(new FileStream(fileName, FileMode.Open, FileAccess.Read),fileDescription.TextEncoding))
            {
                WriteData<T>(values, fileName, sw, fileDescription);
            }
        }

        public void Write<T>(
            IEnumerable<T> values,
            TextWriter stream)
        {
            Write<T>(values, stream, new CsvFileDescription());
        }

        public void Write<T>(
            IEnumerable<T> values,
            string fileName)
        {
            Write<T>(values, fileName, new CsvFileDescription());
        }

        public void Write<T>(
            IEnumerable<T> values,
            TextWriter stream,
            CsvFileDescription fileDescription)
        {
            WriteData<T>(values, null, stream, fileDescription);
        }

        private void WriteData<T>(
            IEnumerable<T> values,
            string fileName,
            TextWriter stream,
            CsvFileDescription fileDescription)
        {
            FieldMapper<T> fm = new FieldMapper<T>(fileDescription, fileName, true);
            CsvStream cs = new CsvStream(null, stream, fileDescription.SeparatorChar);

            List<string> row = new List<string>();
            
            if (fileDescription.FirstLineHasColumnNames)
            {
                fm.WriteNames(ref row);
                cs.WriteRow(row, fileDescription.QuoteAllFields);
            }

            // -----

            foreach (T obj in values)
            {
                // Convert obj to row
                fm.WriteObject(obj, ref row);
                cs.WriteRow(row, fileDescription.QuoteAllFields);
            }
        }

       
        public CsvContext()
        {
        }
    }
}
