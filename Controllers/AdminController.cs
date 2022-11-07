using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Tienda.Data;
using Tienda.Models;

namespace Tienda.Controllers
{
    //Solo los usuarios en la base de datos que tengan el rol AspNetRoles 1, que es Administrator, pueden acceder.
    [Authorize(Roles = "Administrator")]
    public class AdminController : Controller
    {
        //Contexto de la base de datos products
        private readonly ApplicationDbContext _context;
        //Contexto de la ruta actual
        private readonly IWebHostEnvironment _webHostEnvironment;


        public AdminController(ApplicationDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: Admin
        public async Task<IActionResult> Index()
        {
              return View(await _context.Products.ToListAsync());
        }
    

        // GET: Admin/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Admin/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Name,Category,Description,Price,ImageUrl")] Products products)
        {
            if (ModelState.IsValid)
            {
                _context.Add(products);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(products);
        }

        // GET: Admin/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Products == null)
            {
                return NotFound();
            }

            var products = await _context.Products.FindAsync(id);
            if (products == null)
            {
                return NotFound();
            }
            return View(products);
        }

        // POST: Admin/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Name,Category,Description,Price,ImageUrl")] Products products)
        {
            if (id != products.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(products);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProductsExists(products.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(products);
        }

        // GET: Admin/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Products == null)
            {
                return NotFound();
            }

            var products = await _context.Products
                .FirstOrDefaultAsync(m => m.Id == id);
            if (products == null)
            {
                return NotFound();
            }

            return View(products);
        }

        // POST: Admin/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Products == null)
            {
                return Problem("Entity set 'ApplicationDbContext.Products'  is null.");
            }
            var products = await _context.Products.FindAsync(id);
            if (products != null)
            {
                var product = _context.Products.AsNoTracking().FirstOrDefault(p => p.Id == products.Id);
                string webRootPath = _webHostEnvironment.WebRootPath;
                string currentPath= webRootPath + WC.ImagePath;

                var imagenAnterior = Path.Combine(currentPath, product.ImageUrl);
                if (System.IO.File.Exists(imagenAnterior))
                {
                    System.IO.File.Delete(imagenAnterior);
                }
                _context.Products.Remove(products);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool ProductsExists(int id)
        {
          return _context.Products.Any(e => e.Id == id);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Upsert(Products products)
        {
            if (ModelState.IsValid)
            {
                var files = HttpContext.Request.Form.Files;
                string webRootPath = _webHostEnvironment.WebRootPath;
                if (products.Id == 0)
                {
                    //Se crea un nombre para el archivo donde se guardara en la ruta de la carpeta products
                    string upload = webRootPath + WC.ImagePath;
                    string fileName = Guid.NewGuid().ToString();
                    string extension = Path.GetExtension(files[0].FileName);

                    //Se guarda la imagen dentro de la carpeta products del servidor
                    using (var fileStream = new FileStream(Path.Combine(upload, fileName + extension), FileMode.Create))
                    {
                        files[0].CopyTo(fileStream);
                    }
                    //se guarda solamente la ruta de la imagen en la base de datos
                    products.ImageUrl = fileName + extension;
                    _context.Products.Add(products);
                }
                else
                {
                    var product = _context.Products.AsNoTracking().FirstOrDefault(p => p.Id == products.Id);
                    if (files.Count > 0)//Si se carga nueva imagen, se reemplaza
                    {
                        string currentPath = webRootPath + WC.ImagePath;
                        string fileName = Guid.NewGuid().ToString();
                        string extension = Path.GetExtension(files[0].FileName);

                        //Borra la imagen anterior
                        var previousImage = Path.Combine(currentPath, product.ImageUrl);
                        if (System.IO.File.Exists(previousImage))
                        {
                            System.IO.File.Delete(previousImage);
                        }

                        //Se crea la imagen en la ruta
                        using (var fileStream = new FileStream(Path.Combine(currentPath, fileName + extension), FileMode.Create))
                        {
                            files[0].CopyTo(fileStream);
                        }
                        products.ImageUrl = fileName+extension;
                    }//Si no se carga otra imagen, se deja la misma.
                    else
                    {
                        products.ImageUrl = product?.ImageUrl;
                    }
                    _context.Products.Update(products);
                }
                _context.SaveChanges();
                return RedirectToAction(nameof(Index));
            }
            return View(products);
        }
    }
}
