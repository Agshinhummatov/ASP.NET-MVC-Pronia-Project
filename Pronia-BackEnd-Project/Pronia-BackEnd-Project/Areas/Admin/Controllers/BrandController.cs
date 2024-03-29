﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NuGet.ContentModel;
using Pronia_BackEnd_Project.Areas.Admin.ViewModels;
using Pronia_BackEnd_Project.Data;
using Pronia_BackEnd_Project.Helpers;
using Pronia_BackEnd_Project.Models;
using Pronia_BackEnd_Project.Services;
using Pronia_BackEnd_Project.Services.Interfaces;

namespace Pronia_BackEnd_Project.Areas.Admin.Controllers
{
    [Authorize(Roles = "SuperAdmin,Admin")]
    [Area("Admin")]
    public class BrandController : Controller
    {

        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly IBrandService _brandService;
        public BrandController(AppDbContext context, IWebHostEnvironment env, IBrandService brandService)
        {
            _context = context;
            _env = env;
            _brandService = brandService;
        }


        public async Task<IActionResult> Index()
        {
            IEnumerable<Brand> brands = await _brandService.GetAllAsync();

            return View(brands);
        }





        [HttpGet]
        public async Task<IActionResult> Detail(int? id)
        {

            if (id == null) return BadRequest();

            Brand brand = await _brandService.GetByIdAsync((int)id);

            if (brand is null) return NotFound();


            return View(brand);
        }



        [HttpGet]
        public IActionResult Create()     
        {
            return View();
        }






        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BrandCreateVM brand)
        {
            try
            {

                if (!ModelState.IsValid)    
                {
                    return View();    
                }



                foreach (var photo in brand.Photos)
                {

                    if (!photo.CheckFileType("image/"))
                    {
                        ModelState.AddModelError("Photo", "File type must be image");
                        return View();
                    }

                    if (!photo.CheckFileSize(200))
                    {
                        ModelState.AddModelError("Photo", "Image size must be max 200kb");
                        return View();
                    }



                }


                foreach (var photo in brand.Photos)
                {
                    string fileName = Guid.NewGuid().ToString() + "_" + photo.FileName; 



                    string path = FileHelper.GetFilePath(_env.WebRootPath, "assets/images/website-images", fileName);

                    await FileHelper.SaveFileAsync(path, photo);

                    Brand newBrand = new()  
                    {
                        Image = fileName    
                    };

                    await _context.Brands.AddAsync(newBrand);


                }


                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {

                throw;
            }


        }




        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {

            if (id == null) return BadRequest();

            Brand brand = await _brandService.GetByIdAsync((int)id);

            if (brand is null) return NotFound();

           return View(new BrandEditVM
            {
                Image = brand.Image,
              
            });


        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, BrandEditVM brand)
        {
            try
            {
                if (id == null) return BadRequest();

                Brand dbBrand = await _brandService.GetByIdAsync((int)id); 

                if (dbBrand is null) return NotFound();

              
                

                BrandEditVM model = new()
                {
                    Image = dbBrand.Image,

                };

                if (brand.Photo != null)
                {


                    if (!brand.Photo.CheckFileType("image/"))
                    {
                        ModelState.AddModelError("Photo", "File type must be image");
                        return View(dbBrand);
                    }

                    if (!brand.Photo.CheckFileSize(200))
                    {
                        ModelState.AddModelError("Photo", "Image size must be max 200kb");
                        return View(dbBrand);
                    }

                    string oldPath = FileHelper.GetFilePath(_env.WebRootPath, "assets/images/website-images", dbBrand.Image);

                    FileHelper.DeleteFile(oldPath);

                    string fileName = Guid.NewGuid().ToString() + "_" + brand.Photo.FileName; 

                    string newpath = FileHelper.GetFilePath(_env.WebRootPath, "assets/images/website-images", fileName);



                    await FileHelper.SaveFileAsync(newpath, brand.Photo);

                    dbBrand.Image = fileName;

                }
                else
                {
                    Brand newBrand = new()
                    {
                        Image = dbBrand.Image
                    };
                }


                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));

            }

            catch (Exception ex)
            {

                return RedirectToAction("Error", new { msj = ex.Message }); 
            }


        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int? id)
        {
            try
            {

                if (id == null) return BadRequest();

                Brand brand = await _brandService.GetByIdAsync((int)id);

                if (brand is null) return NotFound();

                


                string path = FileHelper.GetFilePath(_env.WebRootPath, "assets/images/website-images", brand.Image);

                FileHelper.DeleteFile(path);

                _context.Brands.Remove(brand);

                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));

            }
            catch (Exception ex)
            {

                return RedirectToAction("Error", new { msj = ex.Message }); 
            }



        }




        public IActionResult Error(string msj) 
        {
            ViewBag.error = msj; 
            return View();
        }











    }
}
