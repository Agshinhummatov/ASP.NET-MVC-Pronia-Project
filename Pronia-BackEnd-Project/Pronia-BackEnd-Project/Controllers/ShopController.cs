﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pronia_BackEnd_Project.Data;
using Pronia_BackEnd_Project.Helpers;
using Pronia_BackEnd_Project.Models;
using Pronia_BackEnd_Project.Services.Interfaces;
using Pronia_BackEnd_Project.ViewModels;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;

namespace Pronia_BackEnd_Project.Controllers
{
    public class ShopController : Controller
    {
        private readonly AppDbContext _context;

        private readonly IProductService _productService;

        private readonly ICategoryService _categoryService;

        private readonly IColorService _colorService;

        private readonly ITagService _tagService;

        public ShopController(IProductService productService, ICategoryService categoryService, IColorService colorService, ITagService tagService, AppDbContext context)
        {

            _productService = productService;
            _categoryService = categoryService;
            _colorService = colorService;
            _tagService = tagService;
            _context = context;
        }
        public async Task<IActionResult> Index(int page = 1, int take = 5)
        {



            IEnumerable<Category> categories = await _categoryService.GetAllAsync();

            IEnumerable<Color> colors = await _colorService.GetAllAsync();

            IEnumerable<Tag> tags = await _tagService.GetAllAsync();



            IEnumerable<Product> dbproducts = await _productService.GetPaginatedDatas(page, take); //page ve take gonderirik icine hemin methoda yazilibdi Servicde orda qebul edecik 

            /*     List<ProdcutListVM> mappedDatas = GetMappedDatas(dbproducts)*/
            ; // datalari getirir mene

            IEnumerable<Product> products = await _productService.GetAllAsync();
            int pageCount = await GetPageCountAsync(take); //paglerin sayin gosderir methodu asaqida yazmisiq 

            Paginate<Product> paginatedDatas = new(dbproducts, page, pageCount);  /// methodumuz bir generice cixartmisiq Paginate bunda her yerde istifade edecik methoda bizden 1 ci datani isdeyir mappedDatas, 2 ci page yeni curet page  3 cu ise totalPage paglerin sayini gosderen methodu gonderirik icine

            ViewBag.take = take;

            ShopVM model = new()
            {
                Products = products,
                Categories = categories,
                Colors = colors,
                Tags = tags,
                PaginatedDatas = paginatedDatas,


            };


            return View(model);
        }



        private async Task<int> GetPageCountAsync(int take)
        {
            var productCount = await _productService.GetCountAsync();  // bu methoda mene productlarin countunu verir
            return (int)Math.Ceiling((decimal)productCount / take);     /// burda bolurki  product conutumzun nece dene take edirikse o qederde gosdersin yeni asqqidaki 1 2 3 yazir onlarin sayini tapmaq ucun 

            //Math.Ceiling() methodu bizden decimal isdeyir bu neynir tutaqki geldi 3.5 eledi bunu yuvarlasdirsin 4 elesin (int)Math ise biz decimal yazmisiq methdmuzun tipi int di ona casstitng elesin

        }

        // pasingation method 
        //private List<ProdcutListVM> GetMappedDatas(List<Product> products)
        //{
        //    List<ProdcutListVM> mappedDatas = new();

        //    foreach (var product in products)
        //    {
        //        ProdcutListVM prodcutVM = new()
        //        {
        //            Id = product.Id,
        //            Name = product.Name,
        //            Description = product.Description,
        //            Price = product.Price,
        //            Rates = product.Rates,
        //            SaleCount = product.SaleCount,
        //            StockCount = product.StockCount,
        //            Sku = product.Sku,
        //            Information = product.Information,
        //            CategoryName = product.ProductCategories.FirstOrDefault().Category.Name,
        //            MainImage = product.ProductImages.Where(m => m.IsMain).FirstOrDefault()?.Image


        //        };

        //        mappedDatas.Add(prodcutVM);

        //    }


        //    return mappedDatas;

        //}


        [HttpGet]
        public async Task<IActionResult> Detail(int? id)
        {
            if (id is null) return BadRequest();

           

            Product product = await _productService.GetFullDataByIdAsync((int)id); ;


            return View(new ProductDetailVM   // view gonderirik bunlari 
            {

                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Rates = product.Rates,
                SaleCount = product.SaleCount,
                StockCount = product.StockCount,
                Sku = product.Sku,
                Information = product.Information,
                ProductCategories = product.ProductCategories,
                ProductImages = product.ProductImages,
                ProductColors = product.ProductColors,
                ProductSize = product.ProductSize,
                ProductTags = product.ProductTags,
            });


           
        }


        [HttpGet]
        public async Task<IActionResult> GetCategoryProducts(int? id)
        {
            if (id == null) return BadRequest();

            var products = await _context.ProductCategories.Where(m => m.Category.Id == id).Include(m =>m.Product).ThenInclude(m =>m.ProductImages).Select(m => m.Product).ToListAsync();



            return PartialView("_ProductsPartial", products);
        }


        [HttpGet]
        public async Task<IActionResult> GetAllCategoriesProducts()
        {
            var products = await _context.Products.Include(m => m.ProductImages).ToListAsync();

            return PartialView("_ProductsPartial", products);
        }







    }
}
