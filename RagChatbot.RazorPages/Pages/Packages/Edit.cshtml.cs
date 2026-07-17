using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;

namespace RagChatbot.RazorPages.Pages.Packages
{
    [Authorize(Roles = "Admin")]
    public class EditModel : PageModel
    {
        private readonly IPackageService _packageService;

        public EditModel(IPackageService packageService)
        {
            _packageService = packageService;
        }

        [BindProperty]
        public PackageDto Input { get; set; } = new();

        public bool IsNew => Input.Id == 0;

        public IActionResult OnGet(int? id)
        {
            if (id.HasValue)
            {
                var p = _packageService.GetById(id.Value);
                if (p == null) return RedirectToPage("Index");
                Input = p;
            }
            return Page();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid) return Page();

            if (Input.Id == 0) _packageService.Create(Input);
            else _packageService.Update(Input);

            TempData["SuccessMessage"] = "Đã lưu gói.";
            return RedirectToPage("Index");
        }
    }
}
