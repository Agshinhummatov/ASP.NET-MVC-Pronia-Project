﻿using Microsoft.EntityFrameworkCore;
using Pronia_BackEnd_Project.Data;
using Pronia_BackEnd_Project.Models;
using Pronia_BackEnd_Project.Services.Interfaces;

namespace Pronia_BackEnd_Project.Services
{
    
    public class SliderService :ISliderService
    {

        private readonly AppDbContext _context;
        public SliderService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Slider>> GetAll()
        {
            return await _context.Sliders.Where(m => !m.SoftDelete).ToListAsync();
        }
    }
}
