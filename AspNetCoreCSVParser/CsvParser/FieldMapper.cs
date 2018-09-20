using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace AspNetCoreCSVParser
{
    internal class FieldMapper<T>
    {
        protected class TypeFieldInfo : IComparable<TypeFieldInfo>
        {
            public int index = CsvColumnAttribute.mc_DefaultFieldIndex;
            public string name = null;
            public bool canBeNull = true;
            public NumberStyles inputNumberStyle = NumberStyles.Any;
            public string outputFormat = null;
            public bool hasColumnAttribute = false;

            public MemberInfo memberInfo = null;
            public Type fieldType = null;
            public TypeConverter typeConverter = null;
            public MethodInfo parseNumberMethod = null;
            public int CompareTo(TypeFieldInfo other)
            {
                return index.CompareTo(other.index);
            }
        }
        protected TypeFieldInfo[] m_IndexToInfo = null;
        protected Dictionary<string, TypeFieldInfo> m_NameToInfo = null;
        protected CsvFileDescription m_fileDescription;
        protected string m_fileName;
        private TypeFieldInfo AnalyzeTypeField(
                                MemberInfo mi,
                                bool allRequiredFieldsMustHaveFieldIndex,
                                bool allCsvColumnFieldsMustHaveFieldIndex)
        {
            TypeFieldInfo tfi = new TypeFieldInfo();

            tfi.memberInfo = mi;

            if (mi is PropertyInfo)
            {
                tfi.fieldType = ((PropertyInfo)mi).PropertyType;
            }
            else
            {
                tfi.fieldType = ((FieldInfo)mi).FieldType;
            }
            tfi.parseNumberMethod =
                tfi.fieldType.GetMethod("Parse",
                    new Type[] { typeof(String), typeof(NumberStyles), typeof(IFormatProvider) });

            tfi.typeConverter = null;
            if (tfi.parseNumberMethod == null)
            {
                tfi.typeConverter =
                    TypeDescriptor.GetConverter(tfi.fieldType);
            }
            tfi.index = CsvColumnAttribute.mc_DefaultFieldIndex;
            tfi.name = mi.Name;
            tfi.inputNumberStyle = NumberStyles.Any;
            tfi.outputFormat = "";
            tfi.hasColumnAttribute = false;

            foreach (Object attribute in mi.GetCustomAttributes(typeof(CsvColumnAttribute), true))
            {
                CsvColumnAttribute cca = (CsvColumnAttribute)attribute;

                if (!string.IsNullOrEmpty(cca.Name))
                {
                    tfi.name = cca.Name;
                }

                tfi.index = cca.FieldIndex;
                tfi.hasColumnAttribute = true;
                tfi.canBeNull = cca.CanBeNull;
                tfi.outputFormat = cca.OutputFormat;
                tfi.inputNumberStyle = cca.NumberStyle;
            }
            if (allCsvColumnFieldsMustHaveFieldIndex &&
                tfi.hasColumnAttribute &&
                tfi.index == CsvColumnAttribute.mc_DefaultFieldIndex)
            {
                throw new ToBeWrittenButMissingFieldIndexException(
                                typeof(T).ToString(),
                                tfi.name);
            }

            if (allRequiredFieldsMustHaveFieldIndex &&
                (!tfi.canBeNull) &&
                (tfi.index == CsvColumnAttribute.mc_DefaultFieldIndex))
            {
                throw new RequiredButMissingFieldIndexException(
                                typeof(T).ToString(),
                                tfi.name);
            }
            return tfi;
        }
        protected void AnalyzeType(
                        Type type,
                        bool allRequiredFieldsMustHaveFieldIndex,
                        bool allCsvColumnFieldsMustHaveFieldIndex)
        {
            m_NameToInfo.Clear();
            foreach (MemberInfo mi in type.GetMembers(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            {
                if ((mi.MemberType == MemberTypes.Field) ||
                    (mi.MemberType == MemberTypes.Property))
                {
                    TypeFieldInfo tfi =
                        AnalyzeTypeField(
                                mi,
                                allRequiredFieldsMustHaveFieldIndex,
                                allCsvColumnFieldsMustHaveFieldIndex);

                    m_NameToInfo[tfi.name] = tfi;
                }
            }
            int nbrTypeFields = m_NameToInfo.Keys.Count;
            m_IndexToInfo = new TypeFieldInfo[nbrTypeFields];

            int i = 0;
            foreach (KeyValuePair<string, TypeFieldInfo> kvp in m_NameToInfo)
            {
                m_IndexToInfo[i++] = kvp.Value;
            }
            Array.Sort(m_IndexToInfo);
            int lastFieldIndex = Int32.MinValue;
            string lastName = "";
            foreach (TypeFieldInfo tfi in m_IndexToInfo)
            {
                if ((tfi.index == lastFieldIndex) &&
                    (tfi.index != CsvColumnAttribute.mc_DefaultFieldIndex))
                {
                    throw new DuplicateFieldIndexException(
                                typeof(T).ToString(),
                                tfi.name,
                                lastName,
                                tfi.index);
                }

                lastFieldIndex = tfi.index;
                lastName = tfi.name;
            }
        }
        public FieldMapper(CsvFileDescription fileDescription, string fileName, bool writingFile)
        {
            if ((!fileDescription.FirstLineHasColumnNames) &&
                (!fileDescription.EnforceCsvColumnAttribute))
            {
                throw new CsvColumnAttributeRequiredException();
            }
            m_fileDescription = fileDescription;
            m_fileName = fileName;

            m_NameToInfo = new Dictionary<string, TypeFieldInfo>();

            AnalyzeType(
                typeof(T),
                !fileDescription.FirstLineHasColumnNames,
                writingFile && !fileDescription.FirstLineHasColumnNames);
        }
        public void WriteNames(ref List<string> row)
        {
            row.Clear();

            for (int i = 0; i < m_IndexToInfo.Length; i++)
            {
                TypeFieldInfo tfi = m_IndexToInfo[i];

                if (m_fileDescription.EnforceCsvColumnAttribute &&
                        (!tfi.hasColumnAttribute))
                {
                    continue;
                }

                // ----

                row.Add(tfi.name);
            }
        }
        public void WriteObject(T obj, ref List<string> row)
        {
            row.Clear();

            for (int i = 0; i < m_IndexToInfo.Length; i++)
            {
                TypeFieldInfo tfi = m_IndexToInfo[i];

                if (m_fileDescription.EnforceCsvColumnAttribute &&
                        (!tfi.hasColumnAttribute))
                {
                    continue;
                }
                Object objValue = null;

                if (tfi.memberInfo is PropertyInfo)
                {
                    objValue =
                        ((PropertyInfo)tfi.memberInfo).GetValue(obj, null);
                }
                else
                {
                    objValue =
                        ((FieldInfo)tfi.memberInfo).GetValue(obj);
                }
                string resultString = null;
                if (objValue != null)
                {
                    if ((objValue is IFormattable))
                    {
                        resultString =
                            ((IFormattable)objValue).ToString(
                                tfi.outputFormat,
                                m_fileDescription.FileCultureInfo);
                    }
                    else
                    {
                        resultString = objValue.ToString();
                    }
                }
                row.Add(resultString);
            }
        }
    }
    internal class FieldMapper_Reading<T> : FieldMapper<T> where T : ICsvRow, new()
    {
        List<string> rowName;
        public FieldMapper_Reading(
                    CsvFileDescription fileDescription,
                    string fileName,
                    bool writingFile)
            : base(fileDescription, fileName, writingFile)
        {
        }
        public void ReadNames(IDataRow row)
        {
            T obj = new T();
            rowName = new List<string>();
            obj.DynamicResourceList = new List<DynamicResource>();
            for (int i = 0; i < row.Count; i++)
            {
                if (!m_NameToInfo.ContainsKey(row[i].Value)) // error case
                {
                    rowName.Add(row[i].Value);
                }
                else
                {
                    m_IndexToInfo[i] = m_NameToInfo[row[i].Value]; //assignment case 
                    rowName.Add(row[i].Value);
                }
                if (m_fileDescription.EnforceCsvColumnAttribute &&
                    (!m_IndexToInfo[i].hasColumnAttribute))
                {
                    throw new MissingCsvColumnAttributeException(typeof(T).ToString(), row[i].Value, m_fileName);
                }
            }
        }
        public T ReadObject(IDataRow row, AggregatedException ae)
        {
            T obj = new T();
            obj.DynamicResourceList = new List<DynamicResource>();
            List<TypeFieldInfo> list = m_IndexToInfo.ToList();
            for (int i = 0; i < row.Count; i++)
            {
                TypeFieldInfo tfi = list.Where(tf => tf.name == rowName[i]).SingleOrDefault();
                if (tfi != null)
                {
                    if (m_fileDescription.EnforceCsvColumnAttribute &&
                            (!tfi.hasColumnAttribute))
                    {
                        throw new TooManyNonCsvColumnDataFieldsException(typeof(T).ToString(), row[i].LineNbr, m_fileName);
                    }
                    if ((!m_fileDescription.FirstLineHasColumnNames) &&
                            (tfi.index == CsvColumnAttribute.mc_DefaultFieldIndex))
                    {
                        throw new MissingFieldIndexException(typeof(T).ToString(), row[i].LineNbr, m_fileName);
                    }
                    string value = row[i].Value;

                    if (value == null)
                    {
                        if (!tfi.canBeNull)
                        {
                            ae.AddException(
                                new MissingRequiredFieldException(
                                        typeof(T).ToString(),
                                        tfi.name,
                                        row[i].LineNbr,
                                        m_fileName));
                        }
                    }
                    else
                    {
                        try
                        {
                            Object objValue = null;
                            if (tfi.typeConverter != null)
                            {
                                objValue = tfi.typeConverter.ConvertFromString(
                                                null,
                                                m_fileDescription.FileCultureInfo,
                                                value);
                            }
                            else if (tfi.parseNumberMethod != null)
                            {
                                if (!string.IsNullOrWhiteSpace(value))
                                {
                                    value = value.Replace('.', ',');
                                    value = Math.Round(decimal.Parse(value.ToString()), 3).ToString();
                                }
                                if (string.IsNullOrWhiteSpace(value))
                                {
                                    value = "0";
                                }
                                if (value.Contains("E") || value.Contains("e"))
                                {
                                    value = value.Replace('.', ',');
                                    value = Convert.ToDouble(value).ToString();
                                }
                                objValue =
                                    tfi.parseNumberMethod.Invoke(
                                        tfi.fieldType,
                                        new Object[] {
                                    value,
                                    tfi.inputNumberStyle,
                                    m_fileDescription.FileCultureInfo });
                            }
                            else
                            {
                                objValue = value;
                            }

                            if (tfi.memberInfo is PropertyInfo)
                            {
                                ((PropertyInfo)tfi.memberInfo).SetValue(obj, objValue, null);
                            }
                            else
                            {
                                ((FieldInfo)tfi.memberInfo).SetValue(obj, objValue);
                            }
                        }
                        catch (Exception e)
                        {
                            if (e is TargetInvocationException)
                            {
                                e = e.InnerException;
                            }

                            if (e is FormatException)
                            {
                                e = new WrongDataFormatException(
                                        typeof(T).ToString(),
                                        tfi.name,
                                        value,
                                        row[i].LineNbr,
                                        m_fileName,
                                        e);
                            }

                            ae.AddException(e);
                        }
                    }
                }
                else
                {
                    if (!string.IsNullOrWhiteSpace(row[i].Value))
                    {
                        obj.DynamicResourceList.Add(new DynamicResource() { Key = rowName[i], Value = row[i].Value });
                    }
                }

            }
            for (int i = row.Count; i < m_IndexToInfo.Length; i++)
            {
                TypeFieldInfo tfi = m_IndexToInfo[i];

                if (((!m_fileDescription.EnforceCsvColumnAttribute) ||
                     tfi.hasColumnAttribute) &&
                    (!tfi.canBeNull))
                {
                    ae.AddException(
                        new MissingRequiredFieldException(
                                typeof(T).ToString(),
                                tfi.name,
                                row[row.Count - 1].LineNbr,
                                m_fileName));
                }
            }
            return obj;
        }

        private void FillList(TypeFieldInfo tfi, T obj)
        {
            ((PropertyInfo)tfi.memberInfo).SetValue(obj, obj.DynamicResourceList, null);
        }
    }
}
