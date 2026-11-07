using ETICARET.Business.Abstract;
using ETICARET.Entities;
using ETICARET.WebUI.Identity;
using ETICARET.WebUI.Models;
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace ETICARET.WebUI.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private readonly IProductService _productService;
        private readonly IOrderService _orderService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(
            ICartService cartService,
            IProductService productService,
            IOrderService orderService,
            UserManager<ApplicationUser> userManager)
        {
            _cartService = cartService;
            _productService = productService;
            _orderService = orderService;
            _userManager = userManager;
        }
        public IActionResult Index()
        {
            Cart cart = _cartService.GetCartByUserId(_userManager.GetUserId(User));

            return View(new CartModel()
            {
                CartId = cart.Id,
                CartItems = cart.CartItems.Select(i => new CartItemModel()
                {
                    CartItemId = i.Id,
                    Name = i.Product.Name,
                    ProductId = i.ProductId,
                    Price = i.Product.Price,
                    Quantity = i.Quantity,
                    ImageUrl = i.Product.Images[0].ImageUrl
                }).ToList()
            });
        }

        public IActionResult AddToCart(int productId,int quantity)
        {
            _cartService.AddToCart(_userManager.GetUserId(User),productId,quantity);
            return RedirectToAction("Index");
        }

        public IActionResult DeleteFromCart(int productId)
        {
            _cartService.DeleteFromCart(_userManager.GetUserId(User), productId);
            return RedirectToAction("Index");
        }

        public IActionResult Checkout()
        {
            var cart = _cartService.GetCartByUserId(_userManager.GetUserId(User));

            OrderModel orderModel = new OrderModel()
            {
                CartModel = new CartModel()
                {
                    CartId = cart.Id,
                    CartItems = cart.CartItems.Select(i => new CartItemModel()
                    {
                        CartItemId = i.Id,
                        Name = i.Product.Name,
                        ProductId = i.ProductId,
                        Price = i.Product.Price,
                        Quantity = i.Quantity,
                        ImageUrl = i.Product.Images[0].ImageUrl
                    }).ToList()
                }
            };

            return View(orderModel);
        }

        [HttpPost]
        public async Task<IActionResult> Checkout(OrderModel model,string paymentMethod)
        {
            ModelState.Remove("CartModel");

            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);
                var cart = _cartService.GetCartByUserId(userId);

                model.CartModel = new CartModel()
                {
                    CartId = cart.Id,
                    CartItems = cart.CartItems.Select(i => new CartItemModel()
                    {
                        CartItemId = i.Id,
                        Name = i.Product.Name,
                        ProductId = i.ProductId,
                        Price = i.Product.Price,
                        Quantity = i.Quantity,
                        ImageUrl = i.Product.Images[0].ImageUrl
                    }).ToList()
                };

                if(paymentMethod == "credit")
                {
                    // Ödeme servisine giden method burada çağrılacak ve ödeme işlemi yapılacak.
                    var payment = PaymentProccess(model);

                    
                    // Ödeme işlemleri ve sipariş kaydı burada yapılacak.
                }
                else
                {
                    // Sipariş kaydı yapılacak.
                }
            }
        }

        private async Task<Payment> PaymentProccess(OrderModel model)
        {
            Options options = new Options()
            {
                BaseUrl = "https://sandbox-api.iyzipay.com",
                ApiKey = "sandbox-cNnJEaoyNt0sCREL4nOq8PajTLQwWeXz",
                SecretKey = "sandbox-cmJxJfaGlVarqNV3c5ZQcMTwVNh8qswx"
            };

            string externalIpString = new WebClient().DownloadString("https://www.icanhazip.com/")
                .Replace("\\r\\n","").Replace("\\n","").Trim();
            IPAddress externalIp = IPAddress.Parse(externalIpString);

            CreatePaymentRequest request = new CreatePaymentRequest();

            request.Locale = Locale.TR.ToString();
            request.ConversationId = Guid.NewGuid().ToString();
            request.Price = model.CartModel.CartItems.Sum(i => i.Price * i.Quantity).ToString().Split(",")[0];
            request.PaidPrice = model.CartModel.CartItems.Sum(i => i.Price * i.Quantity).ToString().Split(",")[0];
            request.Currency = Currency.TRY.ToString();
            request.Installment = 1;
            request.BasketId = model.CartModel.CartId.ToString();
            request.PaymentGroup = PaymentGroup.PRODUCT.ToString();
            request.PaymentChannel = PaymentChannel.WEB.ToString();

            PaymentCard paymentCard = new PaymentCard()
            {
                CardHolderName = model.CardName,
                CardNumber = model.CardNumber,
                ExpireMonth = model.ExprationMonth,
                ExpireYear = model.ExprationYear,
                Cvc = model.CVV,
                RegisterCard = 0
            };

            request.PaymentCard = paymentCard;

            Buyer buyer = new Buyer()
            {
                Id = _userManager.GetUserId(User),
                Name = model.FirstName,
                Surname = model.LastName,
                GsmNumber = model.Phone,
                Email = model.Email,
                IdentityNumber = "11111111111",
                LastLoginDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                RegistrationDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                RegistrationAddress = model.Address,
                Ip = externalIp.ToString(),
                City = model.City,
                Country = model.City,
                ZipCode = "340000"
            };

            request.Buyer = buyer;

            Address address = new Address()
            {
                ContactName = model.FirstName + " " + model.LastName,
                City = model.City,
                Country = model.City,
                Description = model.Address,
                ZipCode = "340000"
            };

            request.ShippingAddress = address;
            request.BillingAddress = address;

            List<BasketItem> basketItems = new List<BasketItem>();
            BasketItem basketItem;

            foreach (var cartItem in model.CartModel.CartItems)
            {
                basketItem = new BasketItem()
                {
                    Id = cartItem.ProductId.ToString(),
                    Name = cartItem.Name,
                    Category1 = _productService.GetProductDetail(cartItem.ProductId).ProductCategories
                    .FirstOrDefault().CategoryId.ToString(),
                    ItemType = BasketItemType.PHYSICAL.ToString(),
                    Price = (cartItem.Price * cartItem.Quantity).ToString().Split(",")[0]
                };

                basketItems.Add(basketItem);
            }

            request.BasketItems = basketItems;

            Payment payment = await Payment.Create(request,options);

            return payment;

        }
    }
}
