using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NG_Core_Auth.Data;
using NG_Core_Auth.Models;

namespace NG_Core_Auth.Controllers
{
    [Route("api/[controller]")]
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProductController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("[action]")]
        [Authorize(Policy = "RequireLoggedIn")]
        public IActionResult GetProducts()
        {
            return Ok(_context.Products.ToList());
        }

        [HttpPost("[action]")]
        [Authorize(Policy = "RequireAdministratorRole")]
        public async Task<IActionResult> AddProduct([FromBody] ProductModel model)
        {
            var newProduct = new ProductModel
            {
                Name = model.Name,
                ImageUrl = model.ImageUrl,
                Description = model.Description,
                OutOfStock = model.OutOfStock,
                Price = model.Price
            };

            await _context.Products.AddAsync(newProduct);
            await _context.SaveChangesAsync();

            return Ok(new JsonResult("The Product added"));
        }

        [HttpPut("[action]/{id}")]
        [Authorize(Policy = "RequireAdministratorRole")]
        public async Task<IActionResult> UpdateProduct([FromRoute] int id, [FromBody] ProductModel model)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var findProduct = _context.Products.FirstOrDefault(p => p.ProductId == id);
            
            if(findProduct == null)
            {
                return NotFound();
            }    

            findProduct.Name = model.Name;
            findProduct.Description = model.Description;
            findProduct.ImageUrl = model.ImageUrl;
            findProduct.Price = model.Price;
            findProduct.OutOfStock = model.OutOfStock;
            
            _context.Entry(findProduct).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return Ok(new JsonResult("The Product with id " + id + "is updated"));
        }

        [HttpDelete("[action]/{id}")]
        [Authorize(Policy = "RequireAdministratorRole")]
        public async Task<IActionResult> DeleteProduct([FromRoute] int id)
        {
            if(!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var findProduct = await _context.Products.FindAsync(id);
            
            if(findProduct == null)
            {
                return NotFound();
            }    
 
            
            _context.Remove(findProduct);
            await _context.SaveChangesAsync();

            return Ok(new JsonResult("The Product with id " + id + "is deleted"));
        }
    }
}