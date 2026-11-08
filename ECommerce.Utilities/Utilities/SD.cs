using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ECommerce.Utilities.Utilities
{
    public static class SD
    {
        public const string Role_Customer = "Customer";
        public const string Role_Vendor = "Vendor";
        public const string Role_Admin = "Admin";
        public const string Role_SuperAdmin = "SuperAdmin";

        public const string SessionCartCount = "SessionCartCount";

        public const string OrderStatus_Pending = "Pending";
        public const string OrderStatus_Approved = "Approved";
        public const string OrderStatus_InProcess = "Processing";
        public const string OrderStatus_Shipped = "Shipped";
        public const string OrderStatus_Cancelled = "Cancelled";
        public const string OrderStatus_Refunded = "Refunded";

        public const string PaymentStatus_Unpaid = "Unpaid";
        public const string PaymentStatus_Paid = "Paid";
        public const string PaymentStatus_Failed = "Failed";
        public const string PaymentStatus_Refunded = "Refunded";
        public const string PaymentStatus_PartiallyPaid = "PartiallyPaid";
        public const string PaymentStatus_Pending = "Pending";

        public const decimal ShippingCost_Free = 0.00m;
        public const decimal ShippingCost_Standard = 50.00m;  
        public const decimal ShippingCost_Express = 100.00m;  
        public const decimal ShippingCost_International = 200.00m;

        public const string CheckoutSessionCompleted = "checkout.session.completed";
        public const string PaymentIntentSucceeded = "payment_intent.succeeded";
        public const string PaymentIntentFailed = "payment_intent.payment_failed";
    }
}
