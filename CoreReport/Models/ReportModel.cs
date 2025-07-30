using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations.Schema;

namespace CoreReport.Models
{
    public class ReportModel
    {
        public int Id { get; set; }
        public string ReportName { get; set; }
        public string SP_Name { get; set; }
        [NotMapped]
        [BindNever]
        public string Parameters { get; set; }
      // You can store the parameters in a simple string, or create a list if needed.
        
        public List<Parameters> ParameterList { get; set; }
    }

    public class Parameters
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public bool IsRequired { get; set; }
    }

}
