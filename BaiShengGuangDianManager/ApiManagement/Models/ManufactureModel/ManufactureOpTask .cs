namespace ApiManagement.Models.ManufactureModel
{
    public class ManufactureOpTask
    {
        public int TaskId { get; set; }
        public string Account { get; set; }
    }
    public class ManufactureOpCheckTask : ManufactureOpTask
    {
        public int CheckResult { get; set; }
    }
}
