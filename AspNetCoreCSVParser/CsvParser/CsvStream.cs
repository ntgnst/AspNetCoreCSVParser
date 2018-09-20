using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AspNetCoreCSVParser
{
    internal class CsvStream
    {
        private TextReader m_instream;
        private TextWriter m_outStream;
        private char m_SeparatorChar;
        private char[] m_SpecialChars;
        private int m_lineNbr;
        public CsvStream(TextReader inStream, TextWriter outStream, char SeparatorChar)
        {
            m_instream = inStream;
            m_outStream = outStream;
            m_SeparatorChar = SeparatorChar;
            m_SpecialChars = ("\"\x0A\x0D" + m_SeparatorChar.ToString()).ToCharArray();
            m_lineNbr = 1;
        }
        public void WriteRow(List<string> row, bool quoteAllFields)
        {
            bool firstItem = true;
            foreach (string item in row)
            {
                if (!firstItem) { m_outStream.Write(m_SeparatorChar); }

                if (item != null)
                {

                    if ((quoteAllFields ||
                        (item.IndexOfAny(m_SpecialChars) > -1) ||
                        (item.Trim() == "")))
                    {
                        m_outStream.Write("\"" + item.Replace("\"", "\"\"") + "\"");
                    }
                    else
                    {
                        m_outStream.Write(item);
                    }
                }

                firstItem = false;
            }

            m_outStream.WriteLine("");
        }
        public bool ReadRow(ref IDataRow row)
        {
            row.Clear();

            while (true)
            {
                int startingLineNbr = m_lineNbr;

                string item = null;

                bool moreAvailable = GetNextItem(ref item);
                if (!moreAvailable)
                {
                    return (row.Count > 0);
                }
                row.Add(new DataRowItem(item, startingLineNbr));
            }
        }

        private bool EOS = false;
        private bool EOL = false;
        private bool previousWasCr = false;

        private bool GetNextItem(ref string itemString)
        {
            itemString = null;
            if (EOL)
            {
                EOL = false;
                return false;
            }

            bool itemFound = false;
            bool quoted = false;
            bool predata = true;
            bool postdata = false;
            StringBuilder item = new StringBuilder();

            while (true)
            {
                char c = GetNextChar(true);
                if (EOS)
                {
                    if (itemFound) { itemString = item.ToString(); }
                    return itemFound;
                }
                if ((!previousWasCr) && (c == '\x0A'))
                {
                    m_lineNbr++;
                }

                if (c == '\x0D')
                {
                    m_lineNbr++;
                    previousWasCr = true;
                }
                else
                {
                    previousWasCr = false;
                }
                if ((postdata || !quoted) && c == m_SeparatorChar)
                {
                    if (itemFound) { itemString = item.ToString(); }
                    return true;
                }

                if ((predata || postdata || !quoted) && (c == '\x0A' || c == '\x0D'))
                {
                    EOL = true;
                    if (c == '\x0D' && GetNextChar(false) == '\x0A')
                    {
                        GetNextChar(true);
                    }

                    if (itemFound) { itemString = item.ToString(); }
                    return true;
                }

                if (predata && c == ' ')
                    continue;

                if (predata && c == '"')
                {
                    quoted = true;
                    predata = false;
                    itemFound = true;
                    continue;
                }

                if (predata)
                {
                    predata = false;
                    item.Append(c);
                    itemFound = true;
                    continue;
                }

                if (c == '"' && quoted)
                {
                    if (GetNextChar(false) == '"')
                    {
                        item.Append(GetNextChar(true));
                    }
                    else
                    {
                        postdata = true;
                    }

                    continue;
                }

                item.Append(c);
            }
        }

        private char[] buffer = new char[4096];
        private int pos = 0;
        private int length = 0;

        private char GetNextChar(bool eat)
        {
            if (pos >= length)
            {
                length = m_instream.ReadBlock(buffer, 0, buffer.Length);
                if (length == 0)
                {
                    EOS = true;
                    return '\0';
                }
                pos = 0;
            }
            if (eat)
                return buffer[pos++];
            else
                return buffer[pos];
        }
    }
}
