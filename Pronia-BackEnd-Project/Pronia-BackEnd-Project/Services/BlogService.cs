﻿using Microsoft.EntityFrameworkCore;
using Pronia_BackEnd_Project.Data;
using Pronia_BackEnd_Project.Models;
using Pronia_BackEnd_Project.Services.Interfaces;

namespace Pronia_BackEnd_Project.Services
{
    public class BlogService : IBlogService
    {

        private readonly AppDbContext _context;
        public BlogService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Blog>> GetAllAsync() => await _context.Blogs.Include(m=>m.Images).Include(m => m.Author).ToListAsync();

        public async Task<Blog> GetByIdAsync(int id) => await _context.Blogs.FindAsync(id);

        public async Task<Blog> GetFullDataByIdAsync(int id) => await _context.Blogs.Include(m => m.Images).Include(m => m.Author)?.FirstOrDefaultAsync(m => m.Id == id);




        public async Task<IEnumerable<Blog>> GetPaginatedDatas(int page, int take)  
        {
            return await _context.Blogs.Include(m => m.Images).Include(m => m.Author)?.Skip((page * take) - take).Take(take).ToListAsync();


        }



        public async Task<int> GetCountAsync()   
        {
            return await _context.Blogs.CountAsync(); 
        }

    }
}
