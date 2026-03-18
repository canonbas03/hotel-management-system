using HotelMVCPrototype.Data;
using HotelMVCPrototype.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize(Roles = "Reception, Admin")]
public class ReceptionRequestItemsController : Controller
{
    private readonly ApplicationDbContext _context;

    public ReceptionRequestItemsController(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
        => View(await _context.RequestItems.ToListAsync());

    public IActionResult Create() => View();

    [HttpPost]
    public async Task<IActionResult> Create(CreateRequestItemViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var item = new RequestItem
        {
            Name = model.Name,
            IsActive = model.IsActive,
            MaxQuantity = model.MaxQuantity

        };

        if (model.Image != null)
        {
            item.ImagePath = await SaveImage(model.Image);
        }

        _context.RequestItems.Add(item);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }


    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var item = await _context.RequestItems.FindAsync(id);
        if (item == null) return NotFound();

        var model = new CreateRequestItemViewModel
        {
            Name = item.Name,
            IsActive = item.IsActive,
            MaxQuantity = item.MaxQuantity

        };

        ViewBag.ItemId = item.Id;
        ViewBag.ExistingImage = item.ImagePath;

        return View(model);
    }
    [HttpPost]
    public async Task<IActionResult> Edit(int id, CreateRequestItemViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var item = await _context.RequestItems.FindAsync(id);
        if (item == null) return NotFound();

        item.Name = model.Name;
        item.IsActive = model.IsActive;
        item.MaxQuantity = model.MaxQuantity;

        if (model.Image != null)
        {
            item.ImagePath = await SaveImage(model.Image);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.RequestItems.FindAsync(id);
        if (item == null) return NotFound();

        _context.RequestItems.Remove(item);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

    private async Task<string> SaveImage(IFormFile image)
    {
        var fileName = Guid.NewGuid() + Path.GetExtension(image.FileName);
        var path = Path.Combine(
            Directory.GetCurrentDirectory(),
            "wwwroot/images/requests",
            fileName
        );

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        using var stream = new FileStream(path, FileMode.Create);
        await image.CopyToAsync(stream);

        return "/images/requests/" + fileName;
    }
}
