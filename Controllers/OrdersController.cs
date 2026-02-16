using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using OrderManagementSystem.Data;
using OrderManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using OrderManagementSystem.Services;

namespace OrderManagementSystem.Controllers
{
    [Authorize]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;

        private readonly S3Service _s3Service;
        public OrdersController(ApplicationDbContext context, S3Service s3Service)
        {
            _context = context;
            _s3Service = s3Service;

        }

        // GET: Orders
        

    public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            IQueryable<Order> ordersQuery = _context.Orders.Include(o => o.CreatedByUser);

            if (!User.IsInRole("Admin") && !User.IsInRole("Manager"))
            {
                ordersQuery = ordersQuery.Where(o => o.CreatedByUserId == userId);
            }

            return View(await ordersQuery.ToListAsync());
        }


    // GET: Orders/Details/5

        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
                return NotFound();

            var order = await _context.Orders
                .FirstOrDefaultAsync(m => m.Id == id);

            if (order == null)
                return NotFound();

            // Generate pre-signed URL if file exists

            if (!string.IsNullOrEmpty(order.FileKey))
            {
                ViewBag.FileUrl = _s3Service.GetPreSignedUrl(
                    order.FileKey,
                    order.OriginalFileName
                );
            }

            return View(order);
        }


        // GET: Orders/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: Orders/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        //public async Task<IActionResult> Create([Bind("ProductName,Quantity,Price,OrderDate")] Order order)
        public async Task<IActionResult> Create(Order order, IFormFile? file)
        {
            order.CreatedByUserId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            // Prevent validation error for CreatedByUserId
            ModelState.Remove("CreatedByUserId");

            if (ModelState.IsValid)
            {
                // Only Admin can upload
                if (file != null && User.IsInRole("Admin"))
                {
                    // File size limit 2MB
                    if (file.Length > 2 * 1024 * 1024)
                    {
                        ModelState.AddModelError("File", "File size must be less than 2MB.");
                        return View(order);
                    }

                    // Allowed file types
                    var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                    var extension = Path.GetExtension(file.FileName).ToLower();

                    if (!allowedExtensions.Contains(extension))
                    {
                        ModelState.AddModelError("File", "Only PDF, JPG, and PNG files are allowed.");
                        return View(order);
                    }

                    // Upload to S3 and store only the key
                    var fileKey = await _s3Service.UploadFileAsync(file);
                    order.FileKey = fileKey;
                    order.OriginalFileName = file.FileName;
                }

                _context.Add(order);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            return View(order);
        }


        // GET: Orders/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }
            return View(order);
        }

        // POST: Orders/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ProductName,Quantity,Price,OrderDate")] Order order)
        {
            if (id != order.Id)
                return NotFound();

            var existingOrder = await _context.Orders.FindAsync(id);
            if (existingOrder == null)
                return NotFound();

            if (existingOrder.Status != OrderStatus.Pending)
            {
                return RedirectToAction(nameof(Index));
            }

            // Update only editable fields
            existingOrder.ProductName = order.ProductName;
            existingOrder.Quantity = order.Quantity;
            existingOrder.Price = order.Price;
            existingOrder.OrderDate = order.OrderDate;

            // DO NOT TOUCH CreatedByUserId

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }



        // GET: Orders/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(m => m.Id == id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Orders/Delete/5
        [Authorize(Roles = "Admin")]
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                _context.Orders.Remove(order);
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool OrderExists(int id)
        {
            return _context.Orders.Any(e => e.Id == id);
        }

        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Approve(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound();
            order.Status = OrderStatus.Approved;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> Reject(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
                return NotFound();
            order.Status = OrderStatus.Rejected;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

    }
}
