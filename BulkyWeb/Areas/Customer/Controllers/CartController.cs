using Bulky.DataAcces.Repository;
using Bulky.DataAcces.Repository.IRepository;
using Bulky.Model;
using Bulky.Model.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System;
using Bulky.Utility;
using Stripe.Checkout;
using System.Data;

namespace BulkyWeb.Areas.Customer.Controllers
{
	[Area("Customer")]
	[Authorize]
	public class CartController : Controller
	{
		[BindProperty]
		public ShoppingCartVM ShoppingCartVM { get; set; }
		private readonly IUnitOfWork _unitOfWork;

		public CartController(IUnitOfWork unitOfWork)
		{
			_unitOfWork = unitOfWork;
		}

		public IActionResult Index()
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var userIdClaim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
			if (userIdClaim == null)
			{
				// Handle the case where the claim is not found
				return RedirectToAction("Index", "Home"); // Or an appropriate error action
			}
			var userId = userIdClaim.Value;

			ShoppingCartVM = new()
			{
				ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product"),
				OrderHeeaader = new OrderHeeaader(),
			};
			IEnumerable<ProductImage> productImages = _unitOfWork.ProductImage.GetAll();

			foreach (var cart in ShoppingCartVM.ShoppingCartList)
			{
				cart.Product.ProductImages = productImages.Where(u => u.ProductId == cart.Product.Id).ToList();
				cart.Price = GetPriceBasedOnQuantity(cart);
				ShoppingCartVM.OrderHeeaader.OrderTotal += (cart.Price * cart.Count);
			}

			return View(ShoppingCartVM);
		}

		public IActionResult Plus(int CartId)
		{
			var CartFromDb = _unitOfWork.ShoppingCart.Get(c => c.Id == CartId);
			CartFromDb.Count++;
			_unitOfWork.ShoppingCart.Update(CartFromDb);
            HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart.GetAll(c => c.ApplicationUserId == CartFromDb.ApplicationUserId).Count());
			_unitOfWork.Save();
            return RedirectToAction("Index");
		}

		public IActionResult Minus(int CartId)
		{
			var CartFromDb = _unitOfWork.ShoppingCart.Get(c => c.Id == CartId);
			if (CartFromDb.Count > 1)
			{
				CartFromDb.Count--;
				_unitOfWork.ShoppingCart.Update(CartFromDb);
				_unitOfWork.Save();
			}
			else
			{
				Remove(CartId);
                HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart.GetAll(c => c.ApplicationUserId == CartFromDb.ApplicationUserId).Count() - 1);
            }
            return RedirectToAction("Index");
		}

		public IActionResult Remove(int CartId)
		{
			var CartFromDb = _unitOfWork.ShoppingCart.Get(c => c.Id == CartId);
			_unitOfWork.ShoppingCart.Remove(CartFromDb);
            HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart.GetAll(c => c.ApplicationUserId == CartFromDb.ApplicationUserId).Count()-1);
            _unitOfWork.Save();
			return RedirectToAction("Index");
		}

		public IActionResult Summary()
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var userIdClaim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
			if (userIdClaim == null)
			{
				// Handle the case where the claim is not found
				return RedirectToAction("Index", "Home"); // Or an appropriate error action
			}
			var userId = userIdClaim.Value;

			ShoppingCartVM = new()
			{
				ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product"),
				OrderHeeaader = new OrderHeeaader(),
			};

			var applicationUser = _unitOfWork.ApplicationUser.Get(a => a.Id == userId);
			if (applicationUser == null)
			{
				// Handle the case where the user is not found
				return RedirectToAction("Index", "Home"); // Or an appropriate error action
			}

			ShoppingCartVM.OrderHeeaader.ApplicationUser = applicationUser;
			ShoppingCartVM.OrderHeeaader.Name = applicationUser.Name;
			ShoppingCartVM.OrderHeeaader.PhoneNumber = applicationUser.PhoneNumber;
			ShoppingCartVM.OrderHeeaader.City = applicationUser.City;
			ShoppingCartVM.OrderHeeaader.StreetAddress = applicationUser.StreetAddress;
			ShoppingCartVM.OrderHeeaader.State = applicationUser.State;
			ShoppingCartVM.OrderHeeaader.PostalCode = applicationUser.PostCode;

			foreach (var cart in ShoppingCartVM.ShoppingCartList)
			{
				cart.Price = GetPriceBasedOnQuantity(cart);
				ShoppingCartVM.OrderHeeaader.OrderTotal += (cart.Price * cart.Count);
			}

			return View(ShoppingCartVM);
		}

		[HttpPost]
		[ActionName("Summary")]
		public IActionResult SummaryPost()
		{
			var claimsIdentity = (ClaimsIdentity)User.Identity;
			var userIdClaim = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier);
			if (userIdClaim == null)
			{
				// Handle the case where the claim is not found
				return RedirectToAction("Index", "Home"); // Or an appropriate error action
			}
			var userId = userIdClaim.Value;

			ShoppingCartVM.ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId, includeProperties: "Product");
			ShoppingCartVM.OrderHeeaader.OrderDate = DateTime.Now;
			ShoppingCartVM.OrderHeeaader.ApplicationUserId = userId;

			var applicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);
			if (applicationUser == null)
			{
				// Handle the case where the user is not found
				return RedirectToAction("Index", "Home"); // Or an appropriate error action
			}

			// Fill the OrderHeader details
			ShoppingCartVM.OrderHeeaader.Name = applicationUser.Name ?? "Default Name";
			ShoppingCartVM.OrderHeeaader.PhoneNumber = applicationUser.PhoneNumber;
			ShoppingCartVM.OrderHeeaader.City = applicationUser.City;
			ShoppingCartVM.OrderHeeaader.StreetAddress = applicationUser.StreetAddress;
			ShoppingCartVM.OrderHeeaader.State = applicationUser.State;
			ShoppingCartVM.OrderHeeaader.PostalCode = applicationUser.PostCode;

			foreach (var cart in ShoppingCartVM.ShoppingCartList)
			{
				cart.Price = GetPriceBasedOnQuantity(cart);
				ShoppingCartVM.OrderHeeaader.OrderTotal += (cart.Price * cart.Count);
			}

			if (applicationUser.CompanyId.GetValueOrDefault() == 0)
			{
				// it is a regular customer
				ShoppingCartVM.OrderHeeaader.PaymentStatus = SD.PaymentStatusPending;
				ShoppingCartVM.OrderHeeaader.OrderStatus = SD.StatusPending;
			}
			else
			{
				// it is a company user
				ShoppingCartVM.OrderHeeaader.PaymentStatus = SD.PaymentStatusDelayedPayment;
				ShoppingCartVM.OrderHeeaader.OrderStatus = SD.StatusApproved;
			}

			_unitOfWork.OrderHeeaader.Add(ShoppingCartVM.OrderHeeaader);
			_unitOfWork.Save();

			foreach (var cart in ShoppingCartVM.ShoppingCartList)
			{
				OrderDetail orderDetail = new()
				{
					ProductId = cart.ProductId,
					OrderHeaderId = ShoppingCartVM.OrderHeeaader.Id,
					price = cart.Price,
					count = cart.Count,
				};
				_unitOfWork.OrderDetails.Add(orderDetail);
				_unitOfWork.Save();
			}

			if (applicationUser.CompanyId.GetValueOrDefault() == 0)
			{
				var domain = "https://localhost:44359/";
				// it is a regular customer
				var options = new SessionCreateOptions
				{
					SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={ShoppingCartVM.OrderHeeaader.Id}",
					CancelUrl = domain + $"customer/cart/index",
					LineItems = new List<SessionLineItemOptions>(),
					Mode = "payment",
				};

				foreach (var item in ShoppingCartVM.ShoppingCartList)
				{
					var sessionLineItem = new SessionLineItemOptions
					{
						PriceData = new SessionLineItemPriceDataOptions
						{
							UnitAmount = (long)(item.Price * 100),
							Currency = "usd",
							ProductData = new SessionLineItemPriceDataProductDataOptions
							{
								Name = item.Product.Title
							}
						},
						Quantity = item.Count
					};
					options.LineItems.Add(sessionLineItem);
				}

				var service = new SessionService();
				Session session = service.Create(options);
				_unitOfWork.OrderHeeaader.UpdateStripePaymentID(ShoppingCartVM.OrderHeeaader.Id, session.Id, session.PaymentIntentId);
				_unitOfWork.Save();
				Response.Headers.Add("Location", session.Url);
				return new StatusCodeResult(303);
			}

			return RedirectToAction(nameof(OrderConfirmation), new { id = ShoppingCartVM.OrderHeeaader.Id });
		}

		public IActionResult OrderConfirmation(int Id)
		{
			var orderHeader = _unitOfWork.OrderHeeaader.Get(u => u.Id == Id, includeProperties: "ApplicationUser");
			if (orderHeader == null)
			{
				return NotFound();  
			}

			if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment)
			{
				var service = new SessionService();
				Session session = service.Get(orderHeader.SessionId);
				if (session.PaymentStatus.ToLower() == "paid")
				{
					_unitOfWork.OrderHeeaader.UpdateStripePaymentID(orderHeader.Id, session.Id, session.PaymentIntentId);
					_unitOfWork.OrderHeeaader.UpdateStatus(orderHeader.Id, SD.StatusApproved, SD.PaymentStatusApproved);
					_unitOfWork.Save();
				}
				HttpContext.Session.Clear();
			}

			var ShoppingCarts = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId).ToList();
			_unitOfWork.ShoppingCart.RemoveRange(ShoppingCarts);
			_unitOfWork.Save();

			return View(Id);
		}

		private double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
		{
			if (shoppingCart.Count <= 50)
			{
				return shoppingCart.Product.Price;
			}
			else if (shoppingCart.Count <= 100)
			{
				return shoppingCart.Product.Price50;
			}
			else
			{
				return shoppingCart.Product.Price100;
			}
		}
	}
}
