namespace AspNetCoreCSVParser
{
    public interface IDataRow
    {
        int Count { get; }
        
        void Clear();
        
        void Add(DataRowItem item);
        
        DataRowItem this[int index] { get; set; }
    }
}
