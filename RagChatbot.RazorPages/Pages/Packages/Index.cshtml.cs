using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RagChatbot.BLL.DTOs;
using RagChatbot.BLL.Services.Interfaces;

namespace RagChatbot.RazorPages.Pages.Packages
{
    [Authorize(Roles = "Admin")]
    public class IndexModel : PageModel
    {
        private readonly IPackageService _packageService;

        public IndexModel(IPackageService packageService)
        {
            _packageService = packageService;
        }

        public List<PackageDto> Packages { get; set; } = new();

        public void OnGet() => Packages = _packageService.GetAll().ToList();

        public IActionResult OnPostToggle(int id)
        {
            _packageService.ToggleActive(id);
            return RedirectToPage();
        }
    }
}
