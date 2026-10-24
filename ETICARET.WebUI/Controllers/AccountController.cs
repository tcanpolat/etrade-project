using ETICARET.Business.Abstract;
using ETICARET.WebUI.EmailService;
using ETICARET.WebUI.Extensions;
using ETICARET.WebUI.Identity;
using ETICARET.WebUI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ETICARET.WebUI.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ICartService _cartService;

        public AccountController(UserManager<ApplicationUser> userManager,SignInManager<ApplicationUser> signInManager,ICartService cartService)
        {
            _cartService = cartService;
            _signInManager = signInManager;
            _userManager = userManager;
        }
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = new ApplicationUser()
            {
                FullName = model.FullName,
                UserName = model.UserName,
                Email = model.Email
            };

            var result = await _userManager.CreateAsync(user,model.Password);

            if (result.Succeeded)
            {
                // Email confirmation token oluşturma
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                var callbackUrl = Url.Action("ConfirmEmail", "Account",
                    new
                    {
                        userId = user.Id,
                        token = code
                    }
                );

                string siteUrl = "https://localhost:7120";
                string activeUrl = $"{siteUrl}{callbackUrl}"; // https://localhost:7120/Account/ConfirmEmail?userId=1&token=fmafjkalkfljkajfkla

                string body = $"Hesabınızı aktifleştirmek için <a href='{activeUrl}'>tıklayınız</a>";

                // Email servisi ile email gönderme işlemi yapılacak.
                var sonuc = MailHelper.SendEmail(body, user.Email, "Hesabınızı Aktifleştirin");

                if (sonuc)
                {
                    TempData.Put("message", new ResultModel()
                    {
                        Title ="Hesap Aktifleştirme",
                        Message = "Mail Adresinize aktifleştirme linki gönderilmiştir. Hesabınızı onaylamak için linke tıklayınız.",
                        Css = "success"
                    });
                }

                return RedirectToAction("Login","Account");
            }
            
            TempData.Put("message", new ResultModel()
            {
                Title ="Kayıt Hatası",
                Message = "Kullanıcı kaydınız oluşturulurken bir hata oluştu!",
                Css = "danger"
            });
            
            return View(model);
        }

        public async Task<IActionResult> ConfirmEmail(string userId,string token)
        {
            if(userId == null || token == null)
            {
                TempData.Put("message", new ResultModel()
                {
                    Title ="Geçersiz Email",
                    Message = "Hesabınız aktifleştirilemedi!",
                    Css = "danger"
                });

                return RedirectToAction("Index", "Home");
            }

            var user = await _userManager.FindByIdAsync(userId);

            if(user != null)
            {
                var result = await _userManager.ConfirmEmailAsync(user, token); // EmailComfirmedı 1 yap

                if (result.Succeeded)
                {
                    // kullanıcının maili başarıyla confir edildiyse sepeti oluşturulacak
                    _cartService.InitialCart(user.Id);
                    TempData.Put("message", new ResultModel()
                    {
                        Title ="Hesabınız aktifleştirme.",
                        Message = "Hesabınız başarıyla aktifleştirildi",
                        Css = "success"
                    });
                    return RedirectToAction("Login", "Account");
                }
            
            }

            TempData.Put("message", new ResultModel()
            {
                Title ="Hesabınız aktifleştirme hatası",
                Message = "Hesabınız aktifleştirilirken bir hata oluştu.Lütfen tekrar deneyiniz",
                Css = "danger"
            });

            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Login(string returnUrl = null)
        {
            return View(
                new LoginModel()
                {
                    ReturnUrl = returnUrl
                }    
            );
        }
    }
}
