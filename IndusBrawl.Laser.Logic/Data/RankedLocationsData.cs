namespace IndusBrawl.Laser.Logic.Data
{
    public class RankedLocationsData : LogicData
    {
        public RankedLocationsData(Row row, DataTable datatable) : base(row, datatable)
        {
            LoadData(this, GetType(), row);
        }

        public string Name { get; set; }
    }
}
