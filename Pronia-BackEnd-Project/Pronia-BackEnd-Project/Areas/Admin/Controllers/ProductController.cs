﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Pronia_BackEnd_Project.Areas.Admin.ViewModels;
using Pronia_BackEnd_Project.Data;
using Pronia_BackEnd_Project.Helpers;
using Pronia_BackEnd_Project.Helpers.Enums;
using Pronia_BackEnd_Project.Models;
using Pronia_BackEnd_Project.Services;
using Pronia_BackEnd_Project.Services.Interfaces;
using Pronia_BackEnd_Project.ViewModels;
using System.Text.RegularExpressions;

namespace Pronia_BackEnd_Project.Areas.Admin.Controllers
{

    [Authorize(Roles = "SuperAdmin,Admin")]
    [Area("Admin")]
    public class ProductController : Controller
    {
        private readonly IProductService _productService;

        private readonly ICategoryService _categoryService;

        private readonly ISizeService _sizeService;

        private readonly ITagService _tagService;

        private readonly IColorService _colorService;

        private readonly IWebHostEnvironment _env;      

        private readonly AppDbContext _context;
        public ProductController(IProductService productService,
                                 ICategoryService categoryService,
                                       IWebHostEnvironment env,
            AppDbContext context, 
            ISizeService sizeService,
            ITagService tagService,
            IColorService colorService)
        {
            _productService = productService;
            _categoryService = categoryService;
            _env = env;
            _context = context;
            _sizeService = sizeService;
            _tagService = tagService;
            _colorService = colorService;
        }




        public async Task<IActionResult> Index(int page = 1, int take = 5)  
        {
            List<Product> products = await _productService.GetPaginatedDatas(page, take); 

            List<ProductListVM> mappedDatas = GetMappedDatas(products); 

            int pageCount = await GetPageCountAsync(take); 

            Paginate<ProductListVM> paginatedDatas = new(mappedDatas, page, pageCount);  

            ViewBag.take = take;

            return View(paginatedDatas);
        }

       

        private async Task<int> GetPageCountAsync(int take)
        {
            var productCount = await _productService.GetCountAsync();  
            return (int)Math.Ceiling((decimal)productCount / take);    
        }

   
        private List<ProductListVM> GetMappedDatas(List<Product> products)
        {
            List<ProductListVM> mappedDatas = new();

            foreach (var product in products)
            {
                ProductListVM prodcutVM = new()
                {
                    Id = product.Id,
                    Name = product.Name,
                    Price = product.Price,
                    Count = product.StockCount,
                    CategoryName = product.ProductCategories.Select(m =>m.Category).FirstOrDefault()?.Name,
                    MainImage = product.ProductImages.Where(m => m.IsMain).FirstOrDefault()?.Image
                };
                mappedDatas.Add(prodcutVM);
            }

            return mappedDatas;
        }



        [HttpGet]
        public async Task<IActionResult> Create()
        {
            ViewBag.categories = await GetCategoriesAsync();

            ViewBag.sizes = await GetSiziesAsync();

            ViewBag.colors = await GetColorsAsync();

            ViewBag.tags = await GetTagsAsync();

            return View();
        }




        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCreateVM model)
        {
            try
            {
                IEnumerable<Product> products = await _productService.GetAllAsync();


                ViewBag.categories = await GetCategoriesAsync();

                ViewBag.tags = await GetTagsAsync();

                ViewBag.sizes = await GetSiziesAsync();

                ViewBag.colors = await GetColorsAsync();

                if (!ModelState.IsValid)
                {
                    return View(model); 
                }


                foreach (var photo in model.Photos)
                {

                    if (!photo.CheckFileType("image/"))
                    {
                        ModelState.AddModelError("Photos", "File type must be image");
                        return View();
                    }

                    if (!photo.CheckFileSize(200))
                    {
                        ModelState.AddModelError("Photos", "Image size must be max 200kb");
                        return View();
                    }



                }

                List<ProductTag> tags = new();

                foreach (var tagId in model.ProductTags)
                {

                    ProductTag tag = new()
                    {
                        TagId = tagId
                    };

                    tags.Add(tag);
                }


                List<ProductColor> colors = new();

                foreach (var colorId in model.ProductColors)
                {

                    ProductColor color = new()
                    {
                        ColorId = colorId
                    };

                    colors.Add(color);
                }


                List<ProductSize> sizes = new();

                foreach (var sizeId in model.ProductSize)
                {

                    ProductSize size = new()
                    {
                        SizeId = sizeId
                    };

                    sizes.Add(size);
                }


                List<ProductCategory> categories = new();

                foreach (var categpryId in model.ProductCategories)
                {

                    ProductCategory category = new()
                    {
                        CategoryId = categpryId
                    };

                    categories.Add(category);
                }


                List<ProductImage> productImages = new();  

                foreach (var photo in model.Photos)
                {
                    string fileName = Guid.NewGuid().ToString() + "_" + photo.FileName; 


                    string path = FileHelper.GetFilePath(_env.WebRootPath, "assets/images/website-images", fileName);

                    await FileHelper.SaveFileAsync(path, photo);


                    ProductImage productImage = new()
                    {
                        Image = fileName
                    };

                    productImages.Add(productImage); 

                }

                productImages.FirstOrDefault().IsMain = true; 

                decimal convertedPrice = decimal.Parse(model.Price); 

                Product newProduct = new()
                {
                    Name = model.Name,      
                    Price = convertedPrice, 
                    SaleCount = model.SaleCount, 
                    Sku = model.Sku,
                    Rates = model.Rates,
                    StockCount = model.StockCount,
                    Information = model.Information,
                    Description = model.Description,
                    ProductCategories = categories,
                    ProductSize = sizes,
                    ProductColors = colors,
                    ProductTags = tags,
                    ProductImages = productImages,
                    
                    
                };

                await _context.ProductImages.AddRangeAsync(productImages); 
                await _context.Products.AddAsync(newProduct);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (Exception)
            {

                throw;
            }

        }



        [HttpPost]
        [ValidateAntiForgeryToken]

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return BadRequest();

            Product product = await _productService.GetFullDataByIdAsync((int)id);

            if (product is null) return NotFound();


            foreach (var item in product.ProductImages)
            {
                string path = FileHelper.GetFilePath(_env.WebRootPath, "assets/images/website-images", item.Image);
                FileHelper.DeleteFile(path);

            }

            _context.Products.Remove(product);

            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));

        }



        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return BadRequest();

            Product dbProduct = await _productService.GetFullDataByIdAsync((int)id);

            if (dbProduct is null) return NotFound();


            ViewBag.categories = await GetCategoriesAsync();

            ViewBag.sizes = await GetSiziesAsync();

            ViewBag.colors = await GetColorsAsync();

            ViewBag.tags = await GetTagsAsync();


            return View(new ProductEditVM
            {   Id = dbProduct.Id,
                Name = dbProduct.Name,
                Description = dbProduct.Description,
                Price = dbProduct.Price.ToString("0.#####").Replace(",", "."),
                Rates = dbProduct.Rates,
                SaleCount = dbProduct.SaleCount,
                StockCount = dbProduct.StockCount,
                Sku = dbProduct.Sku,
                Information = dbProduct.Information,
                Images = dbProduct.ProductImages,
                ProductCategoriesId = dbProduct.ProductCategories.Select(pc => pc.CategoryId).ToList(),
                ProductColorsId = dbProduct.ProductColors.Select(pc => pc.ColorId).ToList(),
                ProductSizeId = dbProduct.ProductSize.Select(ps => ps.SizeId).ToList(),
                ProductTagsId = dbProduct.ProductTags.Select(pt => pt.TagId).ToList(),

            }); 

        }






        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int? id, ProductEditVM updatedProduct)
        {
            ViewBag.categories = await GetCategoriesAsync();

            ViewBag.sizes = await GetSiziesAsync();

            ViewBag.colors = await GetColorsAsync();

            ViewBag.tags = await GetTagsAsync();


            if (!ModelState.IsValid) return View(updatedProduct);

            Product dbProduct = await _productService.GetFullDataByIdAsync((int)id);

            if (dbProduct is null) return NotFound();

            List<ProductTag> tags = new();

            foreach (var tagId in updatedProduct.ProductTagsId)
            {

                ProductTag tag = new()
                {
                    TagId = tagId
                };

                tags.Add(tag);
            }


            List<ProductColor> colors = new();

            foreach (var colorId in updatedProduct.ProductColorsId)
            {

                ProductColor color = new()
                {
                    ColorId = colorId
                };

                colors.Add(color);
            }


            List<ProductSize> sizes = new();

            foreach (var sizeId in updatedProduct.ProductSizeId)
            {

                ProductSize size = new()
                {
                    SizeId = sizeId
                };

                sizes.Add(size);
            }


            List<ProductCategory> categories = new();

            foreach (var categpryId in updatedProduct.ProductCategoriesId)
            {

                ProductCategory category = new()
                {
                    CategoryId = categpryId
                };

                categories.Add(category);
            }

            if (updatedProduct.Photos != null)
            {

                foreach (var photo in updatedProduct.Photos)
                {

                    if (!photo.CheckFileType("image/"))
                    {
                        ModelState.AddModelError("Photo", "File type must be image");
                        return View(dbProduct);
                    }

                    if (!photo.CheckFileSize(200))
                    {
                        ModelState.AddModelError("Photo", "Image size must be max 200kb");
                        return View(dbProduct);
                    }

                }

                foreach (var item in dbProduct.ProductImages)
                {

                    string path = FileHelper.GetFilePath(_env.WebRootPath, "assets/images/website-images", item.Image);

                    FileHelper.DeleteFile(path);
                }



                List<ProductImage> productImages = new();  

                foreach (var photo in updatedProduct.Photos)
                {
                    string fileName = Guid.NewGuid().ToString() + "_" + photo.FileName; 


                    string path = FileHelper.GetFilePath(_env.WebRootPath, "assets/images/website-images", fileName);

                    await FileHelper.SaveFileAsync(path, photo);


                    ProductImage productImage = new()   
                    {
                        Image = fileName
                    };

                    productImages.Add(productImage); 

                }

                productImages.FirstOrDefault().IsMain = true;

                dbProduct.ProductImages = productImages;
            }


            decimal convertedPrice = decimal.Parse(updatedProduct.Price);


            dbProduct.Name = updatedProduct.Name;
            dbProduct.Description = updatedProduct.Description;
            dbProduct.Price = convertedPrice;
            dbProduct.Sku = updatedProduct.Sku;
            dbProduct.SaleCount = updatedProduct.SaleCount;
            dbProduct.Rates = updatedProduct.Rates;
            dbProduct.Information = updatedProduct.Information;
            dbProduct.StockCount = updatedProduct.StockCount;
            dbProduct.ProductCategories = categories;
            dbProduct.ProductSize = sizes;
            dbProduct.ProductColors = colors;
            dbProduct.ProductTags = tags;
            dbProduct.Updated = DateTime.Now;
          
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }




        [HttpGet]
        public async Task<IActionResult> Detail(int? id)
        {
            if (id is null) return BadRequest();

            Product product = await _productService.GetFullDataByIdAsync((int)id);
            if (product is null) return NotFound();



            List<string> images = new List<string>();

            foreach (var image in product.ProductImages)
            {
                images.Add(image.Image);
            }


            List<string> tags = new List<string>();

            foreach (var tag in product.ProductTags.Select(pt => pt.Tag))
            {
                tags.Add(tag.Name);
            }


            List<string> categories = new List<string>();

            foreach (var category in product.ProductCategories.Select(pt => pt.Category))
            {
                categories.Add(category.Name);
            }


            List<string> sizes = new List<string>();

            foreach (var size in product.ProductSize.Select(pt => pt.Size))
            {
                sizes.Add(size.Name);
            }

            List<string> colors = new List<string>();

            foreach (var color in product.ProductColors.Select(pt => pt.Color))
            {
                colors.Add(color.Name);
            }

            ProductDetailsVM model = new ProductDetailsVM
            {
                Name = product.Name,
                Price = product.Price,
                Description = product.Description,
                SaleCount = product.SaleCount,
                StockCount = product.StockCount,
                SKU = product.Sku,
                Rating = product.Rates,
                Images = images,
                Categories = categories,
                ColorName = colors,
                Tags = tags,
                Sizes = sizes,
                CreatedAt = product.Created,
                UpdatedAt = product.Updated
            };

            return View(model);
        }







        private async Task<SelectList> GetCategoriesAsync()
        {

            IEnumerable<Category> categories = await _categoryService.GetAllAsync();

            return new SelectList(categories, "Id", "Name"); 

        }


        private async Task<SelectList> GetSiziesAsync()
        {
            IEnumerable<Size> sizes = await _sizeService.GetAllAsync();

            return new SelectList(sizes, "Id", "Name");
        }


        private async Task<SelectList> GetColorsAsync()
        {
            IEnumerable<Color> colors = await _colorService.GetAllAsync();

            return new SelectList(colors, "Id", "Name"); 

        }



        private async Task<SelectList> GetTagsAsync()
        {
            IEnumerable<Tag> tags = await _tagService.GetAllAsync();

            return new SelectList(tags, "Id", "Name"); 

        }



    }
}
