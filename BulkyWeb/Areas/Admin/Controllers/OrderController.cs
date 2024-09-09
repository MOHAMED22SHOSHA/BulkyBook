using Bulky.DataAcces.Repository;
using Bulky.DataAcces.Repository.IRepository;
using Bulky.Model;
using Bulky.Model.ViewModels;
using Bulky.Utility;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using System.Security.Claims;

namespace BulkyWeb.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _UnitOfWork;
        [BindProperty]
        public OrderVM OrderVM { get; set; }
        public OrderController(IUnitOfWork UnitOfWork)
        {
            _UnitOfWork = UnitOfWork;
        }
        public IActionResult Index()
        {
            return View();
        }
        public IActionResult Details(int OrderId)
        {
            OrderVM orderVM = new()
            {
                OrderHeeaader = _UnitOfWork.OrderHeeaader.Get(u => u.Id == OrderId, includeProperties: "ApplicationUser"),
                OrderDetail = _UnitOfWork.OrderDetails.GetAll(u => u.OrderHeaderId == OrderId, includeProperties: "Product")
            };


            return View(orderVM);
        }



        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult UpdateOrderDetail()
        {
            var orderHeaderFromDb = _UnitOfWork.OrderHeeaader.Get(u => u.Id == OrderVM.OrderHeeaader.Id);
            orderHeaderFromDb.Name = OrderVM.OrderHeeaader.Name;
            orderHeaderFromDb.PhoneNumber = OrderVM.OrderHeeaader.PhoneNumber;
            orderHeaderFromDb.StreetAddress = OrderVM.OrderHeeaader.StreetAddress;
            orderHeaderFromDb.City = OrderVM.OrderHeeaader.City;
            orderHeaderFromDb.State = OrderVM.OrderHeeaader.State;
            orderHeaderFromDb.PostalCode = OrderVM.OrderHeeaader.PostalCode;
            if (!string.IsNullOrEmpty(OrderVM.OrderHeeaader.Carrier))
            {
                orderHeaderFromDb.Carrier = OrderVM.OrderHeeaader.Carrier;
            }
            if (!string.IsNullOrEmpty(OrderVM.OrderHeeaader.TrackingNumber))
            {
                orderHeaderFromDb.Carrier = OrderVM.OrderHeeaader.TrackingNumber;
            }
            _UnitOfWork.OrderHeeaader.Update(orderHeaderFromDb);
            _UnitOfWork.Save();

            TempData["Success"] = "Order Details Updated Successfully.";


            return RedirectToAction(nameof(Details), new { orderId = orderHeaderFromDb.Id });
        }


        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult StartProcessing()
        {
            _UnitOfWork.OrderHeeaader.UpdateStatus(OrderVM.OrderHeeaader.Id, SD.StatusInProcess);
            _UnitOfWork.Save();
            TempData["Success"] = "Order Details Updated Successfully.";
            return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeeaader.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult ShipOrder()
        {
            var orderHeader = _UnitOfWork.OrderHeeaader.Get(u => u.Id == OrderVM.OrderHeeaader.Id);
            orderHeader.TrackingNumber = OrderVM.OrderHeeaader.TrackingNumber;
            orderHeader.Carrier=OrderVM.OrderHeeaader.Carrier;
            orderHeader.OrderStatus = SD.StatusShipped;
            orderHeader.ShoppingDate=DateTime.Now;
            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                orderHeader.PaymentDueDate= DateTime.Now.AddDays(30);
            }
            _UnitOfWork.OrderHeeaader.UpdateStatus(OrderVM.OrderHeeaader.Id, SD.StatusShipped);
            _UnitOfWork.Save();
            TempData["Success"] = "Order Shipped Successfully.";
            return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeeaader.Id });
        }

        [HttpPost]
        [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
        public IActionResult CancelOrder()
        {
            var orderHeader = _UnitOfWork.OrderHeeaader.Get(u => u.Id == OrderVM.OrderHeeaader.Id);
            if(orderHeader.PaymentStatus==SD.PaymentStatusApproved)
            {
                var options = new RefundCreateOptions
                {
                    Reason = RefundReasons.RequestedByCustomer,
                    PaymentIntent = orderHeader.PaymentIntentId
                };
                var service=new RefundService();
                Refund refund=service.Create(options);
                _UnitOfWork.OrderHeeaader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusRefunded);

            }
            else
            {
                _UnitOfWork.OrderHeeaader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusCancelled);
            }
            _UnitOfWork.Save();
            TempData["Success"] = "Order Cancelled Successfully.";
            return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeeaader.Id });
        }

        [HttpPost]
        [ActionName("Details")]
        public IActionResult Details_Pat_Now()
        {

            OrderVM.OrderHeeaader = _UnitOfWork.OrderHeeaader
                .Get(u => u.Id == OrderVM.OrderHeeaader.Id, includeProperties: "ApplicationUser");
            OrderVM.OrderDetail = _UnitOfWork.OrderDetails
                .GetAll(u => u.OrderHeaderId == OrderVM.OrderHeeaader.Id, includeProperties: "Product");

            //stripe logic
            var domain = Request.Scheme + "://" + Request.Host.Value + "/";
            var options = new SessionCreateOptions
            {
                SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderHeaderId={OrderVM.OrderHeeaader.Id}",
                CancelUrl = domain + $"admin/order/details?orderId={OrderVM.OrderHeeaader.Id}",
                LineItems = new List<SessionLineItemOptions>(),
                Mode = "payment",
            };

            foreach (var item in OrderVM.OrderDetail)
            {
                var sessionLineItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.price * 100), 
                        Currency = "usd",
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = item.Product.Title
                        }
                    },
                    Quantity = item.count
                };
                options.LineItems.Add(sessionLineItem);
            }


            var service = new SessionService();
            Session session = service.Create(options);
            _UnitOfWork.OrderHeeaader.UpdateStripePaymentID(OrderVM.OrderHeeaader.Id, session.Id, session.PaymentIntentId);
            _UnitOfWork.Save();
            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);

        }

        public IActionResult PaymentConfirmation(int orderHeaderId)
        {

            OrderHeeaader orderHeader = _UnitOfWork.OrderHeeaader.Get(u => u.Id == orderHeaderId);
            if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
            {
                var service = new SessionService();
                Session session = service.Get(orderHeader.SessionId);

                if (session.PaymentStatus.ToLower() == "paid")
                {
                    _UnitOfWork.OrderHeeaader.UpdateStripePaymentID(orderHeaderId, session.Id, session.PaymentIntentId);
                    _UnitOfWork.OrderHeeaader.UpdateStatus(orderHeaderId, orderHeader.OrderStatus, SD.PaymentStatusApproved);
                    _UnitOfWork.Save();
                }
            }
            return View(orderHeaderId);
        }
        #region API calls
        [HttpGet]
        public IActionResult GetAll(string status)

        {
            IEnumerable<OrderHeeaader> obj;
            if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Customer)) { 
                 obj = _UnitOfWork.OrderHeeaader.GetAll(includeProperties: "ApplicationUser").ToList();
            }
            else
            {
                var claimsIdentity = (ClaimsIdentity)User.Identity;
                var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;
                obj = _UnitOfWork.OrderHeeaader.GetAll(u => u.ApplicationUserId == userId, includeProperties: "ApplicationUser");
            }
            switch (status)
            {
                case "pending":
                    obj = obj.Where(u => u.PaymentStatus == SD.PaymentStatusDelayedPayment);
                    break;
                case "inprocess":
                    obj = obj.Where(u => u.PaymentStatus == SD.StatusInProcess);
                    break;
                case "completed":
                    obj = obj.Where(u => u.PaymentStatus == SD.StatusShipped);
                    break;
                case "approved":
                    obj = obj.Where(u => u.PaymentStatus == SD.StatusApproved);
                    break;
                default:
                    break;

            }
            return Json(new { data = obj });
        }


        #endregion
    }
}
