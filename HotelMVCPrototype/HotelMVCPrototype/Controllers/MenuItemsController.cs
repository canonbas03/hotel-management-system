using HotelMVCPrototype.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

[Authorize(Roles = "Bar, Admin")]
public class MenuItemsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;

    public MenuItemsController(ApplicationDbContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }


    public async Task<IActionResult> Index()
    {
        var items = await _context.MenuItems
            .OrderBy(m => m.Category)
            .ThenBy(m => m.Name)
            .ToListAsync();

        return View(items);
    }
    public IActionResult Create()
    {
        return View(new MenuItemCreateViewModel());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MenuItemCreateViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        string? imagePath = null;

        if (model.Image != null)
        {
            var uploads = Path.Combine(_env.WebRootPath, "images/menu");
            Directory.CreateDirectory(uploads);

            var fileName = Guid.NewGuid() + Path.GetExtension(model.Image.FileName);
            var filePath = Path.Combine(uploads, fileName);

            using var stream = new FileStream(filePath, FileMode.Create);
            await model.Image.CopyToAsync(stream);

            imagePath = "/images/menu/" + fileName;
        }

        var item = new MenuItem
        {
            Name = model.Name,
            Price = model.Price,
            Category = model.Category,
            IsVegan = model.IsVegan,
            ImagePath = imagePath
        };

        _context.MenuItems.Add(item);
        await _context.SaveChangesAsync();

        return RedirectToAction("Index");
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _context.MenuItems.FindAsync(id);
        if (item == null)
            return NotFound();

        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, MenuItem model, IFormFile? image)
    {
        if (id != model.Id)
            return NotFound();

        if (!ModelState.IsValid)
            return View(model);

        var item = await _context.MenuItems.FindAsync(id);
        if (item == null)
            return NotFound();

        item.Name = model.Name;
        item.Price = model.Price;
        item.Category = model.Category;
        item.IsVegan = model.IsVegan;
        item.IsActive = model.IsActive;

        if (image != null && image.Length > 0)
        {
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(image.FileName)}";
            var path = Path.Combine("wwwroot/images/menu", fileName);

            using var stream = new FileStream(path, FileMode.Create);
            await image.CopyToAsync(stream);

            item.ImagePath = "/images/menu/" + fileName;
        }

        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index));
    }

}
