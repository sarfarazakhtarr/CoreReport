using CoreReport.Models;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace CoreReport.Controllers
{
    public class ReportsController : Controller
    {
        private readonly ReportRepository _repository;

        public ReportsController(ReportRepository repository)
        {
            _repository = repository;
        }

        public IActionResult Index()
        {
            var reports = _repository.GetAll();
            return View(reports);
        }


        [HttpPost]
        public IActionResult CreateOrEdit([FromBody] ReportModel model)
        {
            // Ensure Parameters is serialized before validation
            if (model.ParameterList != null)
            {
                model.Parameters = JsonSerializer.Serialize(model.ParameterList);
            }

            //if (!ModelState.IsValid)
            //{
            //    var errors = ModelState.Values.SelectMany(v => v.Errors)
            //        .Select(e => e.ErrorMessage)
            //        .ToList();

            //    return Json(new { success = false, message = "Invalid input model.", errors });
            //}

            try
            {
                if (model.Id == 0)
                {
                    _repository.Insert(model); // Example insert logic
                }
                else
                {
                    _repository.Update(model); // Example update logic
                }

                return Json(new { success = true, message = "Saved successfully" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult DeleteConfirmed(int id)
        {
            var existing = _repository.GetById(id);
            if (existing == null)
                return Json(new { success = false, message = "Report not found." });

            _repository.Delete(id);
            return Json(new { success = true });
        }
    }
}
