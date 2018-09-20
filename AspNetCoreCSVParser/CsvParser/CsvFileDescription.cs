using System.Globalization;
using System.Text;

namespace AspNetCoreCSVParser
{
    public class CsvFileDescription
    {
        private CultureInfo m_cultureInfo = CultureInfo.CurrentCulture;

        private int m_maximumNbrExceptions = 100;
        public char SeparatorChar { get; set; }
        public bool QuoteAllFields { get; set; }
        public bool FirstLineHasColumnNames { get; set; }
        public bool EnforceCsvColumnAttribute { get; set; }
        public string FileCultureName
        {
            get { return m_cultureInfo.Name; }
            set { m_cultureInfo = new CultureInfo(value); }
        }

        public CultureInfo FileCultureInfo
        {
            get { return m_cultureInfo; }
            set { m_cultureInfo = value; }
        }
        public int MaximumNbrExceptions
        {
            get { return m_maximumNbrExceptions; }
            set { m_maximumNbrExceptions = value; }
        }
        public Encoding TextEncoding { get; set; }
        public bool DetectEncodingFromByteOrderMarks { get; set; }
        public CsvFileDescription()
        {
            m_cultureInfo = CultureInfo.CurrentCulture;
            FirstLineHasColumnNames = true;
            EnforceCsvColumnAttribute = false;
            QuoteAllFields = false;
            SeparatorChar = ',';
            TextEncoding = Encoding.UTF8;
            DetectEncodingFromByteOrderMarks = true;
        }
    }
}
