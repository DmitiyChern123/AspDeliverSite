using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Dadata;
using Dadata.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Entities2;
using WebApplication1.Models;

namespace WebApplication1.Controllers
{
    public class KorzinasController : Controller
    {
        private readonly DiplomContext _context;

        private const double FixedLatitude = 56.84495958999723;
        private const double FixedLongitude = 60.63911005278376;
        //[56.84495958999723,60.63911005278376]
        public KorzinasController(DiplomContext context)
        {
            _context = context;
        }
        [Authorize(Roles = "user")]
        public ActionResult SetLocation(KorzinaViewModel model)
        {

           
            if (ModelState.IsValid)
            {
                // Здесь вы можете сохранить данные заказа в базу данных или выполнить другие действия
                // ...
                Order order = new Order();
                order.Adress = model.Address;
                order.Phone = model.PhoneNumber;
                order.PayType = model.PaymentType;
                order.Date = DateTime.Now;
                order.Phone = model.PhoneNumber.ToString();
                order.StatusId = 1;
                order.Is_need_devices = model.Is_need_devices;
                var username = User.Identity.Name;
                var user = _context.Users.FirstOrDefault(x => x.Name == username);
                var korzinas = _context.Korzinas.Include(d=>d.ProductType) .Where(d=>d.UserId==user.Id).ToList();
                //order.KorzinaInOrders = korzinas; !!!! фикс ит
                int totalsum = 0;
                foreach (var item in  korzinas)
                {
                    totalsum += item.ProductType.Price * item.Count;
                    
                }
                user.BonusPoints = user.BonusPoints+  Convert.ToInt32( totalsum * ( Convert.ToDouble(5) / 100)) ;
                order.UserId = user.Id;

                foreach (var item in korzinas)
                {
                    var newinorder = new KorzinaInOrder();
                    newinorder.Order = order;
                    newinorder.ProductId = item.ProductTypeId;
                    newinorder.Count = item.Count;
                    
                    _context.KorzinaInOrders.Add(newinorder);

                }

                var dd = _context.Korzinas.Where(d => d.UserId == user.Id).ToList();
                foreach (var item in dd)
                {
                    _context.Korzinas.Remove(item);
                }

                _context.Orders.Add(order);
                _context.SaveChanges();
                return View("successOrder");
                // return RedirectToAction("Success"); // Перенаправление на страницу успеха
            }

            // Если модель не валидна, возвращаем ту же самую форму с сообщениями об ошибках
            //
            //return View(model);
            return View("СreateOrder", model);

        }
        [Authorize(Roles = "user")]
        public ActionResult successOrder ()
        {
            return View();
        }
        private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
        {
            double R = 6371; // Радиус Земли в километрах
            double dLat = ToRadians(lat2 - lat1);
            double dLon = ToRadians(lon2 - lon1);
            double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                       Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                       Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            double distance = R * c; // Расстояние в километрах
            return distance * 1000; // Переводим в метры
        }
        private double ToRadians(double degrees)
        {
            return degrees * Math.PI / 180;
        }
        // GET: Korzinas
        [Authorize(Roles = "user")]
        public async Task<IActionResult> Index()
        {
            KorzinaViewModel model = new KorzinaViewModel();
            model.korzinas = _context.Korzinas.Include(d=>d.ProductType).ToList();
            model.products = _context.ProductTypes.ToList();
            return View(model);
        }

        // GET: Korzinas/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null || _context.Korzinas == null)
            {
                return NotFound();
            }

            var korzina = await _context.Korzinas
                .Include(k => k.ProductTypeId)
                .Include(k => k.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (korzina == null)
            {
                return NotFound();
            }

            return View(korzina);
        }
        public IActionResult СreateOrder( KorzinaViewModel model)
        {
            model.Is_need_devices = true;
            return View(model);
        }
     
        [HttpPost]
        public ActionResult UpdateQuantity(int id, int newCount)
        {
            if (newCount>0)
            {
                var d = _context.Korzinas.FirstOrDefault(a=>a.Id == id);
                if (d!=null)
                {
                    d.Count = newCount;
                    _context.SaveChanges();
                }
            }
            // Здесь вы можете обновить количество товара в базе данных
            // Используйте model.ProductId для идентификации товара
            // Используйте model.Quantity для нового количества товара

            return RedirectToAction("Index");
        }
        public ActionResult MinusProduct(int korid,KorzinaViewModel model)
        {
            if (korid != null && korid !=0)
            {
                _context.Korzinas.Remove(_context.Korzinas.FirstOrDefault(d=>d.Id==korid));
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
        // GET: Korzinas/Create
        [HttpPost]
        public IActionResult Create()
        {
            ViewData["ProductId"] = new SelectList(_context.Products, "Id", "Id");
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id");
            return View();
        }

        // POST: Korzinas/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,ProductId,UserId")] Korzina korzina)
        {
            if (ModelState.IsValid)
            {
                _context.Add(korzina);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewData["ProductId"] = new SelectList(_context.Products, "Id", "Id", korzina.ProductTypeId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", korzina.UserId);
            return View(korzina);
        }

        // GET: Korzinas/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null || _context.Korzinas == null)
            {
                return NotFound();
            }

            var korzina = await _context.Korzinas.FindAsync(id);
            if (korzina == null)
            {
                return NotFound();
            }
            ViewData["ProductId"] = new SelectList(_context.Products, "Id", "Id", korzina.ProductTypeId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", korzina.UserId);
            return View(korzina);
        }

        // POST: Korzinas/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,ProductId,UserId")] Korzina korzina)
        {
            if (id != korzina.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(korzina);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!KorzinaExists(korzina.Id))
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
            ViewData["ProductId"] = new SelectList(_context.Products, "Id", "Id", korzina.ProductTypeId);
            ViewData["UserId"] = new SelectList(_context.Users, "Id", "Id", korzina.UserId);
            return View(korzina);
        }

        // GET: Korzinas/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null || _context.Korzinas == null)
            {
                return NotFound();
            }

            var korzina = await _context.Korzinas
                .Include(k => k.ProductTypeId)
                .Include(k => k.User)
                .FirstOrDefaultAsync(m => m.Id == id);
            if (korzina == null)
            {
                return NotFound();
            }

            return View(korzina);
        }

        // POST: Korzinas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (_context.Korzinas == null)
            {
                return Problem("Entity set 'DiplomContext.Korzinas'  is null.");
            }
            var korzina = await _context.Korzinas.FindAsync(id);
            if (korzina != null)
            {
                _context.Korzinas.Remove(korzina);
            }
            
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool KorzinaExists(int id)
        {
          return (_context.Korzinas?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
