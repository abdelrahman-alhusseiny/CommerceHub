using ECommerce.DataAccess.Data;
using ECommerce.DataAccess.Repository.IRepository;
using ECommerce.Models.Models;
using ECommerce.Models.ViewModels;
using ECommerce.Utilities.Utilities;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using Stripe.Climate;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;


namespace ECommerce.DataAccess.Repository
{
    public class OrderHeaderRepository : Repository<OrderHeader>, IOrderHeaderRepository
    {
        private readonly ApplicationDbContext _db;
        public OrderHeaderRepository(ApplicationDbContext db) : base(db)
        {
            _db = db;
        }

        public async Task<(IEnumerable<OrderHeader>, int)> GetFilteredOrderHeadersAsync(
            int? customerId,
            string searchString,
            string orderStatus,
            string currentSort,
            int currentPage,
            int pageSize)
        {
            var query = _db.OrderHeaders
                .Include(oh => oh.Customer)
                .Include(oh => oh.OrderItems)
                .AsQueryable();

            if (customerId.HasValue)
            {
                query = query.Where(oh => oh.CustomerId == customerId.Value);
            }

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                query = query.Where(oh => oh.FullName.Contains(searchString));
            }

            if (!string.IsNullOrWhiteSpace(orderStatus))
            {
                query = query.Where(oh => oh.OrderStatus == orderStatus);
            }

            query = currentSort switch
            {
                "name" => query.OrderBy(oh => oh.FullName),
                "name_desc" => query.OrderByDescending(oh => oh.FullName),
                "totalCost" => query.OrderBy(oh => oh.TotalCost),
                "totalCost_desc" => query.OrderByDescending(oh => oh.TotalCost),
                "orderDate" => query.OrderBy(oh => oh.OrderDate),
                "orderDate_desc" => query.OrderByDescending(oh => oh.OrderDate),
                _ => query.OrderByDescending(oh => oh.OrderDate)
            };

            int totalItems = await query.CountAsync();

            var orderHeaders = await query
                .Skip((currentPage - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (orderHeaders, totalItems);
        }

        public async Task UpdateAsync(OrderHeader order)
        {
            var objFromDb = await _db.OrderHeaders.FirstOrDefaultAsync(
                oh => oh.Id == order.Id);

            if (objFromDb != null)
            {
                objFromDb.FullName = order.FullName;
                objFromDb.PhoneNumber = order.PhoneNumber;
                objFromDb.Email = order.Email;
                objFromDb.StreetAddress1 = order.StreetAddress1;
                objFromDb.StreetAddress2 = order.StreetAddress2;
                objFromDb.City = order.City;
                objFromDb.State = order.State;
                objFromDb.PostalCode = order.PostalCode;
                objFromDb.Country = order.Country;

                objFromDb.SubtotalCost = order.SubtotalCost;
                objFromDb.ShippingCost = order.ShippingCost;
                objFromDb.TotalCost = order.TotalCost;
                objFromDb.OrderStatus = order.OrderStatus;
                objFromDb.PaymentStatus = order.PaymentStatus;

                objFromDb.SessionId = order.SessionId;
                objFromDb.PaymentIntentId = order.PaymentIntentId;

                objFromDb.TrackingNumber = order.TrackingNumber;
                objFromDb.Carrier = order.Carrier;

                objFromDb.ShippingDate = order.ShippingDate;
            }
        }
    }
}
